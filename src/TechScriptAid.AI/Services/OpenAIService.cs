using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;
using Polly;
using Polly.Retry;
using System.Diagnostics;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Interfaces;

namespace TechScriptAid.AI.Services
{
    public class OpenAIService : IAIService
    {
        private readonly ILogger<OpenAIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IAICacheService _cacheService;
        private readonly ITokenCalculator _tokenCalculator;
        private readonly IAIOperationLogger _operationLogger;
        private readonly SemaphoreSlim _rateLimitSemaphore;

        public OpenAIService(
            ILogger<OpenAIService> logger,
            HttpClient httpClient,
            IConfiguration config,
            IAICacheService cacheService,
            ITokenCalculator tokenCalculator,
            IAIOperationLogger operationLogger)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
            _cacheService = cacheService;
            _tokenCalculator = tokenCalculator;
            _operationLogger = operationLogger;


            // Set the authorization header here
            var apiKey = _config["AI:OpenAI:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
            else
            {
                _logger.LogWarning("OpenAI API key is not configured!");
            }
            // Initialize rate limiting
            var maxConcurrent = _config.GetValue<int>("AI:RateLimits:ConcurrentRequests", 10);
            _rateLimitSemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        }

        private async Task<OpenAIResponse> SendOpenAIRequestAsync(
            string systemPrompt,
            string userPrompt,
            string model,
            int maxTokens,
            CancellationToken ct)
        {
            var body = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = maxTokens,
                temperature = 0.7
            };

            var request = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            //  var response = await _httpClient.PostAsync("/chat/completions", request, ct);
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var parsed = JsonDocument.Parse(json);

            var content = parsed.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var usage = parsed.RootElement.GetProperty("usage");

            return new OpenAIResponse
            {
                Content = content!,
                PromptTokens = usage.GetProperty("prompt_tokens").GetInt32(),
                CompletionTokens = usage.GetProperty("completion_tokens").GetInt32(),
                TotalTokens = usage.GetProperty("total_tokens").GetInt32(),
                Model = model
            };
        }

        public async Task<SummarizationResponse> SummarizeAsync(
            SummarizationRequest request,
            CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var operation = new AIOperation
            {
                OperationType = nameof(SummarizeAsync),
                UserId = request.UserId,
                Model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo",
                RequestContent = JsonSerializer.Serialize(request)
            };

            try
            {
                // Check cache
                var cacheKey = _cacheService.GenerateCacheKey(nameof(SummarizeAsync), request);
                var cachedResponse = await _cacheService.GetAsync<SummarizationResponse>(cacheKey);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("Cache hit for summarization request");
                    return cachedResponse;
                }

                // Validate token limits
                var estimatedTokens = _tokenCalculator.EstimateTokens(request.Text);
                if (!_tokenCalculator.IsWithinTokenLimit(request.Text, 4000))
                {
                    throw new InvalidOperationException("Text exceeds token limit");
                }

                // Rate limiting
                await _rateLimitSemaphore.WaitAsync(ct);
                try
                {
                    var systemPrompt = BuildSummarizationSystemPrompt(request.Style);
                    var userPrompt = $"Summarize the following text in approximately {request.MaxSummaryLength} words:\n\n{request.Text}";

                    var openAIResponse = await SendOpenAIRequestAsync(
                        systemPrompt,
                        userPrompt,
                        operation.Model,
                        request.MaxSummaryLength * 2,
                        ct);

                    var result = new SummarizationResponse
                    {
                        Summary = openAIResponse.Content,
                        KeyPoints = ExtractKeyPoints(openAIResponse.Content),
                        ConfidenceScore = 0.95,
                        Model = openAIResponse.Model,
                        Usage = new TokenUsage
                        {
                            PromptTokens = openAIResponse.PromptTokens,
                            CompletionTokens = openAIResponse.CompletionTokens,
                            EstimatedCost = _tokenCalculator.CalculateCost(
                                openAIResponse.PromptTokens,
                                openAIResponse.CompletionTokens,
                                openAIResponse.Model)
                        },
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        CorrelationId = request.CorrelationId
                    };

                    // Cache the response
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

                    // Log operation
                    operation.ResponseContent = JsonSerializer.Serialize(result);
                    operation.PromptTokens = openAIResponse.PromptTokens;
                    operation.CompletionTokens = openAIResponse.CompletionTokens;
                    operation.Cost = result.Usage.EstimatedCost;
                    operation.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    operation.IsSuccessful = true;

                    await _operationLogger.LogOperationAsync(operation);

                    return result;
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in summarization");
                operation.IsSuccessful = false;
                operation.ErrorMessage = ex.Message;
                operation.ErrorCode = ex.GetType().Name;
                operation.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                await _operationLogger.LogOperationAsync(operation);
                throw;
            }
        }

        public async Task<ContentGenerationResponse> GenerateContentAsync(
            ContentGenerationRequest request,
            CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var operation = new AIOperation
            {
                OperationType = nameof(GenerateContentAsync),
                UserId = request.UserId,
                Model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo",
                RequestContent = JsonSerializer.Serialize(request)
            };

            try
            {
                // Check cache
                var cacheKey = _cacheService.GenerateCacheKey(nameof(GenerateContentAsync), request);
                var cachedResponse = await _cacheService.GetAsync<ContentGenerationResponse>(cacheKey);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("Cache hit for content generation request");
                    return cachedResponse;
                }

                await _rateLimitSemaphore.WaitAsync(ct);
                try
                {
                    var systemPrompt = request.SystemPrompt ?? BuildContentGenerationSystemPrompt(request.ContentType);
                    var userPrompt = request.Prompt;

                    // Apply parameters if provided
                    if (request.Parameters != null)
                    {
                        foreach (var param in request.Parameters)
                        {
                            userPrompt = userPrompt.Replace($"{{{param.Key}}}", param.Value);
                        }
                    }

                    var openAIResponse = await SendOpenAIRequestAsync(
                        systemPrompt,
                        userPrompt,
                        operation.Model,
                        request.MaxTokens,
                        ct);

                    var result = new ContentGenerationResponse
                    {
                        GeneratedContent = openAIResponse.Content,
                        Suggestions = GenerateSuggestions(openAIResponse.Content, request.ContentType),
                        Model = openAIResponse.Model,
                        Usage = new TokenUsage
                        {
                            PromptTokens = openAIResponse.PromptTokens,
                            CompletionTokens = openAIResponse.CompletionTokens,
                            EstimatedCost = _tokenCalculator.CalculateCost(
                                openAIResponse.PromptTokens,
                                openAIResponse.CompletionTokens,
                                openAIResponse.Model)
                        },
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        CorrelationId = request.CorrelationId,
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = request.ContentType.ToString(),
                            ["Temperature"] = request.Temperature
                        }
                    };

                    // Cache the response
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(2));

                    // Log operation
                    operation.ResponseContent = JsonSerializer.Serialize(result);
                    operation.PromptTokens = openAIResponse.PromptTokens;
                    operation.CompletionTokens = openAIResponse.CompletionTokens;
                    operation.Cost = result.Usage.EstimatedCost;
                    operation.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    operation.IsSuccessful = true;

                    await _operationLogger.LogOperationAsync(operation);

                    return result;
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in content generation");
                operation.IsSuccessful = false;
                operation.ErrorMessage = ex.Message;
                operation.ErrorCode = ex.GetType().Name;
                operation.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                await _operationLogger.LogOperationAsync(operation);
                throw;
            }
        }

        // Implement remaining interface methods with similar pattern...

        public async Task<TextAnalysisResponse> AnalyzeTextAsync(
            TextAnalysisRequest request,
            CancellationToken ct = default)
        {
            // Similar implementation pattern as above
            var model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
            var openAIResponse = await SendOpenAIRequestAsync(
                "Analyze the following text for tone, sentiment, and entities. Return results in JSON format.",
                request.Text,
                model,
                1000,
                ct);

            return new TextAnalysisResponse { Analysis = openAIResponse.Content };
        }

        public async Task<EmbeddingResponse> GenerateEmbeddingAsync(
            EmbeddingRequest request,
            CancellationToken ct = default)
        {
            var apiKey = _config["AI:OpenAI:ApiKey"];
            var model = request.Model ?? _config["AI:OpenAI:EmbeddingModel"] ?? "text-embedding-ada-002";

            var body = new
            {
                input = request.Texts,
                model = model
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/embeddings");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var parsed = JsonDocument.Parse(json);

            var embeddings = parsed.RootElement.GetProperty("data")
                .EnumerateArray()
                .Select((e, index) => new EmbeddingData
                {
                    Index = index,
                    Vector = e.GetProperty("embedding").EnumerateArray().Select(x => x.GetDouble()).ToArray(),
                    Embedding = e.GetProperty("embedding").EnumerateArray().Select(x => (float)x.GetDouble()).ToArray(),
                    Object = e.TryGetProperty("object", out var objProp) ? objProp.GetString() ?? "embedding" : "embedding"
                })
                .ToArray();

            var usage = parsed.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();

            return new EmbeddingResponse
            {
                Embeddings = embeddings,
                Model = model,
                Usage = new TokenUsage
                {
                    PromptTokens = promptTokens,
                    CompletionTokens = 0,
                    EstimatedCost = _tokenCalculator.CalculateCost(promptTokens, 0, model)
                }
            };
        }

        public async Task<SemanticSearchResponse> SemanticSearchAsync(
            SemanticSearchRequest request,
            CancellationToken ct = default)
        {
            var model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
            var openAIResponse = await SendOpenAIRequestAsync(
                "Search semantically for the most relevant info:",
                request.Query,
                model,
                500,
                ct);

            return new SemanticSearchResponse
            {
                Results = new[]
                {
                    new SearchResult
                    {
                        Score = 0.99,
                        Text = openAIResponse.Content,
                        Content = openAIResponse.Content
                    }
                },
                TotalResults = 1,
                ProcessingTimeMs = 0,
                CorrelationId = request.CorrelationId
            };
        }

        // Helper methods
        private string BuildSummarizationSystemPrompt(SummarizationStyle style)
        {
            return style switch
            {
                SummarizationStyle.Bullet => "You are a professional summarizer. Create concise bullet-point summaries that capture key information.",
                SummarizationStyle.Paragraph => "You are a professional summarizer. Create flowing paragraph summaries that maintain context and readability.",
                SummarizationStyle.Executive => "You are an executive assistant. Create executive summaries focusing on key decisions, actions, and outcomes.",
                SummarizationStyle.Technical => "You are a technical writer. Create summaries that preserve technical accuracy and important details.",
                _ => "You are a professional summarizer. Create balanced summaries that are both informative and concise."
            };
        }

        private string BuildContentGenerationSystemPrompt(ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Technical => "You are a technical content expert. Generate accurate, detailed technical content.",
                ContentType.Creative => "You are a creative writer. Generate engaging, imaginative content.",
                ContentType.Business => "You are a business communication expert. Generate professional, clear business content.",
                ContentType.Academic => "You are an academic writer. Generate well-researched, properly structured academic content.",
                ContentType.Marketing => "You are a marketing expert. Generate persuasive, engaging marketing content.",
                ContentType.Code => "You are an expert programmer. Generate clean, efficient, well-documented code.",
                _ => "You are a versatile content creator. Generate high-quality content appropriate to the context."
            };
        }

        private string[] ExtractKeyPoints(string text)
        {
            // Simple implementation - in production, you might want to use another AI call
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            return sentences.Take(3).Select(s => s.Trim()).ToArray();
        }

        private string[] GenerateSuggestions(string content, ContentType contentType)
        {
            // Simple implementation - in production, you might want to use another AI call
            return new[]
            {
                $"Consider adding more details about the main topic",
                $"Review the {contentType} content for clarity",
                $"Ensure consistency in tone and style"
            };
        }

        private class OpenAIResponse
        {
            public string Content { get; set; } = string.Empty;
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
            public string Model { get; set; } = string.Empty;
        }
    }
}




//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Configuration;
//using TechScriptAid.Core.DTOs.AI;
//using TechScriptAid.Core.Interfaces.AI;
//using Polly;
//using Polly.Retry;
//using Microsoft.Extensions.Caching.Distributed;

//namespace TechScriptAid.AI.Services
//{
//    public class OpenAIService : IAIService
//    {
//        private readonly ILogger<OpenAIService> _logger;
//        private readonly HttpClient _httpClient;
//        private readonly IConfiguration _config;
//        private readonly IDistributedCache _cache;
//        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

//        public OpenAIService(ILogger<OpenAIService> logger, HttpClient httpClient, IConfiguration config, IDistributedCache cache)
//        {
//            _logger = logger;
//            _httpClient = httpClient;
//            _config = config;
//            _cache = cache;

//            _retryPolicy = Policy<HttpResponseMessage>
//                .Handle<HttpRequestException>()
//                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
//                .WaitAndRetryAsync(3, retryAttempt =>
//                {
//                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
//                    _logger.LogWarning($"[Polly Retry] Attempt {retryAttempt} - delaying {delay.TotalSeconds}s");
//                    return delay;
//                });
//        }

//        private async Task<string> SendOpenAIRequestAsync(string systemPrompt, string userPrompt, string model, CancellationToken ct)
//        {
//            var cacheKey = $"openai:summarize:{userPrompt.GetHashCode()}";
//            var cached = await _cache.GetStringAsync(cacheKey, ct);
//            if (!string.IsNullOrEmpty(cached))
//            {
//                _logger.LogInformation("[Redis] Cache hit for summarize request.");
//                return cached;
//            }

//            var baseUrl = _config["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
//            var endpoint = $"{baseUrl}/chat/completions";
//            var apiKey = _config["AI:OpenAI:ApiKey"];
//            var tokenRate = double.TryParse(_config["AI:OpenAI:TokenCostPerThousand"], out var r) ? r : 0.0015;
//            var cacheMinutes = int.TryParse(_config["AI:OpenAI:CacheMinutes"], out var cm) ? cm : 10;

//            var body = new
//            {
//                model = model,
//                messages = new[]
//                {
//                    new { role = "system", content = systemPrompt },
//                    new { role = "user", content = userPrompt }
//                }
//            };

//            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
//            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

//            HttpResponseMessage response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request, ct));
//            response.EnsureSuccessStatusCode();

//            var json = await response.Content.ReadAsStringAsync(ct);
//            var parsed = JsonDocument.Parse(json);

//            if (parsed.RootElement.TryGetProperty("usage", out var usage))
//            {
//                var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
//                var completionTokens = usage.GetProperty("completion_tokens").GetInt32();
//                var totalTokens = usage.GetProperty("total_tokens").GetInt32();
//                var cost = (totalTokens / 1000.0) * tokenRate;

//                _logger.LogInformation($"[OpenAI] prompt: {promptTokens}, completion: {completionTokens}, total: {totalTokens} tokens, est. cost: ${cost:0.0000}");
//            }

//            var content = parsed.RootElement
//                        .GetProperty("choices")[0]
//                        .GetProperty("message")
//                        .GetProperty("content")
//                        .GetString();

//            await _cache.SetStringAsync(cacheKey, content!, new DistributedCacheEntryOptions
//            {
//                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes)
//            }, ct);

//            return content!;
//        }

//        public async Task<SummarizationResponse> SummarizeAsync(SummarizationRequest request, CancellationToken ct = default)
//        {
//            var model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
//            var content = await SendOpenAIRequestAsync("Summarize the user's input.", request.Text, model, ct);
//            return new SummarizationResponse { Summary = content };
//        }

//        public async Task<ContentGenerationResponse> GenerateContentAsync(ContentGenerationRequest request, CancellationToken ct = default)
//        {
//            var model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
//            var content = await SendOpenAIRequestAsync("Generate content based on the following prompt:", request.Prompt, model, ct);
//            return new ContentGenerationResponse { GeneratedContent = content };
//        }

//        public async Task<TextAnalysisResponse> AnalyzeTextAsync(TextAnalysisRequest request, CancellationToken ct = default)
//        {
//            var model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
//            var content = await SendOpenAIRequestAsync("Analyze the following text for tone, sentiment, and entities:", request.Text, model, ct);
//            return new TextAnalysisResponse { Analysis = content };
//        }

//        public async Task<EmbeddingResponse> GenerateEmbeddingAsync(EmbeddingRequest request, CancellationToken ct = default)
//        {
//            var apiKey = _config["AI:OpenAI:ApiKey"];
//            var model = _config["AI:OpenAI:EmbeddingModel"] ?? "text-embedding-ada-002";
//            var baseUrl = _config["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
//            var endpoint = $"{baseUrl}/embeddings";

//            var body = new
//            {
//                input = request.Texts,
//                model = model
//            };

//            var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
//            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
//            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

//            var res = await _httpClient.SendAsync(req, ct);
//            res.EnsureSuccessStatusCode();

//            var json = await res.Content.ReadAsStringAsync(ct);
//            var parsed = JsonDocument.Parse(json);
//            var embeddings = parsed.RootElement.GetProperty("data")
//                .EnumerateArray()
//                .Select((e, index) => new EmbeddingData
//                {
//                    Index = index,
//                    Vector = e.GetProperty("embedding").EnumerateArray().Select(x => x.GetDouble()).ToArray(),
//                    Embedding = e.GetProperty("embedding").EnumerateArray().Select(x => (float)x.GetDouble()).ToArray(),
//                    Object = e.TryGetProperty("object", out var objProp) ? objProp.GetString() ?? "embedding" : "embedding"
//                })
//                .ToArray();

//            return new EmbeddingResponse { Embeddings = embeddings };
//        }

//        public async Task<SemanticSearchResponse> SemanticSearchAsync(SemanticSearchRequest request, CancellationToken ct = default)
//        {
//            var model = _config["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
//            var content = await SendOpenAIRequestAsync("Search semantically for the most relevant info:", request.Query, model, ct);
//            return new SemanticSearchResponse
//            {
//                Results = new[]
//                {
//                    new SearchResult { Score = 0.99, Text = content }
//                }
//            };
//        }
//    }
//}