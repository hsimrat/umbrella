using System;
using System.Diagnostics;
using System.Text.Json;
using Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Polly;
using Polly.CircuitBreaker;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Interfaces;
using TechScriptAid.Core.Interfaces.AI;
// Use aliases to resolve ambiguities
using AIContentType = TechScriptAid.Core.DTOs.AI.ContentType;
using DocumentEmbedding = TechScriptAid.Core.Entities.DocumentEmbedding;
using SearchResult = TechScriptAid.Core.DTOs.AI.SearchResult;

namespace TechScriptAid.AI.Services
{
    /// <summary>
    /// Enterprise-grade Azure OpenAI service implementation
    /// </summary>
    public class AzureOpenAIService : IAIService
    {
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly IAIOperationLogger _operationLogger;
        private readonly IAICacheService _cacheService;
        private readonly ITokenCalculator _tokenCalculator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AIConfiguration _configuration;
        private readonly OpenAIClient _openAIClient;
        private readonly Kernel _kernel;
        private readonly IAsyncPolicy<Response<ChatCompletions>> _retryPolicy;
        private readonly SemaphoreSlim _rateLimitSemaphore;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

        public AzureOpenAIService(
            ILogger<AzureOpenAIService> logger,
            IAIOperationLogger operationLogger,
            IAICacheService cacheService,
            ITokenCalculator tokenCalculator,
            IUnitOfWork unitOfWork,
            IOptions<AIConfiguration> configuration)
        {
            _logger = logger;
            _operationLogger = operationLogger;
            _cacheService = cacheService;
            _tokenCalculator = tokenCalculator;
            _unitOfWork = unitOfWork;
            _configuration = configuration.Value;

            // Initialize OpenAI client
            _openAIClient = new OpenAIClient(
                new Uri(_configuration.Endpoint),
                new AzureKeyCredential(_configuration.ApiKey));

            // Initialize Semantic Kernel - FIXED
            var builder = Kernel.CreateBuilder();
            builder.Services.AddAzureOpenAIChatCompletion(
                deploymentName: _configuration.DeploymentName!,
                endpoint: _configuration.Endpoint,
                apiKey: _configuration.ApiKey);

            _kernel = builder.Build();

            // Configure retry policy
            var retryPolicy = Policy<Response<ChatCompletions>>
                .Handle<RequestFailedException>()
                .OrResult(r => r.GetRawResponse().Status >= 500)
                .WaitAndRetryAsync(
                    _configuration.MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}ms for operation {Operation}",
                            retryCount, timespan.TotalMilliseconds, context.OperationKey);
                    });

            // Configure circuit breaker
            var circuitBreakerPolicy = Policy<Response<ChatCompletions>>
                .Handle<RequestFailedException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (result, timespan) =>
                    {
                        _logger.LogError(
                            "Circuit breaker opened for {Duration} minutes",
                            timespan.TotalMinutes);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset");
                    });

            // Combine policies
            _retryPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);


            // Initialize rate limiting
            _rateLimitSemaphore = new SemaphoreSlim(
                _configuration.RateLimits.ConcurrentRequests,
                _configuration.RateLimits.ConcurrentRequests);
        }

        public AzureOpenAIService(ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;
            _circuitBreaker = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));
        }

        public async Task<string> GetSummaryAsync(string input)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Calling Azure AI for summarization...");

                // Simulated delay and dummy response
                await Task.Delay(500);
                return $"Summary of: {input.Substring(0, Math.Min(50, input.Length))}...";
            });
        }

        public CircuitState GetCircuitState()
        {
            return _circuitBreaker.CircuitState;
        }

        public async Task<SummarizationResponse> SummarizeAsync(
            SummarizationRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var operation = new AIOperation
            {
                OperationType = nameof(SummarizeAsync),
                UserId = request.UserId,
                Model = _configuration.DeploymentName!,
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
                if (!_tokenCalculator.IsWithinTokenLimit(request.Text, 8000))
                {
                    throw new InvalidOperationException("Text exceeds token limit");
                }

                // Rate limiting
                await _rateLimitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Build prompt based on style
                    var systemPrompt = BuildSummarizationSystemPrompt(request.Style);
                    var userPrompt = $"Summarize the following text in approximately {request.MaxSummaryLength} words:\n\n{request.Text}";

                    // Create chat completion options
                    var chatCompletionsOptions = new ChatCompletionsOptions
                    {
                        DeploymentName = _configuration.DeploymentName,
                        Messages =
                        {
                            new ChatRequestSystemMessage(systemPrompt),
                            new ChatRequestUserMessage(userPrompt)
                        },
                        Temperature = 0.3f,
                        MaxTokens = request.MaxSummaryLength * 2,
                        NucleusSamplingFactor = 0.95f,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0
                    };

                    // Execute with retry policy
                    var response = await _retryPolicy.ExecuteAsync(async () =>
                        await _openAIClient.GetChatCompletionsAsync(
                            chatCompletionsOptions,
                            cancellationToken));

                    var completion = response.Value;
                    var summary = completion.Choices[0].Message.Content;

                    // Extract key points using Semantic Kernel
                    var keyPoints = await ExtractKeyPointsAsync(summary, cancellationToken);

                    var result = new SummarizationResponse
                    {
                        Summary = summary,
                        KeyPoints = keyPoints,
                        ConfidenceScore = CalculateConfidenceScore(completion),
                        Model = completion.Model,
                        Usage = new TokenUsage
                        {
                            PromptTokens = completion.Usage.PromptTokens,
                            CompletionTokens = completion.Usage.CompletionTokens,
                            EstimatedCost = _tokenCalculator.CalculateCost(
                                completion.Usage.PromptTokens,
                                completion.Usage.CompletionTokens,
                                completion.Model)
                        },
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        CorrelationId = request.CorrelationId
                    };

                    // Cache the response
                    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

                    // Log operation
                    operation.ResponseContent = JsonSerializer.Serialize(result);
                    operation.PromptTokens = completion.Usage.PromptTokens;
                    operation.CompletionTokens = completion.Usage.CompletionTokens;
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
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var operation = new AIOperation
            {
                OperationType = nameof(GenerateContentAsync),
                UserId = request.UserId,
                Model = _configuration.DeploymentName!,
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

                await _rateLimitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Get or build system prompt
                    var systemPrompt = request.SystemPrompt ?? BuildContentGenerationSystemPrompt(request.ContentType);

                    // Apply parameters if provided
                    var userPrompt = request.Prompt;
                    if (request.Parameters != null)
                    {
                        foreach (var param in request.Parameters)
                        {
                            userPrompt = userPrompt.Replace($"{{{param.Key}}}", param.Value);
                        }
                    }

                    var chatCompletionsOptions = new ChatCompletionsOptions
                    {
                        DeploymentName = _configuration.DeploymentName,
                        Messages =
                        {
                            new ChatRequestSystemMessage(systemPrompt),
                            new ChatRequestUserMessage(userPrompt)
                        },
                        Temperature = (float)request.Temperature,
                        MaxTokens = request.MaxTokens,
                        NucleusSamplingFactor = 0.95f,
                        FrequencyPenalty = 0.1f,
                        PresencePenalty = 0.1f
                    };

                    var response = await _retryPolicy.ExecuteAsync(async () =>
                        await _openAIClient.GetChatCompletionsAsync(
                            chatCompletionsOptions,
                            cancellationToken));

                    var completion = response.Value;
                    var generatedContent = completion.Choices[0].Message.Content;

                    // Generate suggestions for improvement
                    var suggestions = await GenerateSuggestionsAsync(generatedContent, request.ContentType, cancellationToken);

                    var result = new ContentGenerationResponse
                    {
                        GeneratedContent = generatedContent,
                        Suggestions = suggestions,
                        Model = completion.Model,
                        Usage = new TokenUsage
                        {
                            PromptTokens = completion.Usage.PromptTokens,
                            CompletionTokens = completion.Usage.CompletionTokens,
                            EstimatedCost = _tokenCalculator.CalculateCost(
                                completion.Usage.PromptTokens,
                                completion.Usage.CompletionTokens,
                                completion.Model)
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
                    operation.PromptTokens = completion.Usage.PromptTokens;
                    operation.CompletionTokens = completion.Usage.CompletionTokens;
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

        public async Task<TextAnalysisResponse> AnalyzeTextAsync(
            TextAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var operation = new AIOperation
            {
                OperationType = nameof(AnalyzeTextAsync),
                UserId = request.UserId,
                Model = _configuration.DeploymentName!,
                RequestContent = JsonSerializer.Serialize(request)
            };

            try
            {
                await _rateLimitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var analysisPrompt = BuildTextAnalysisPrompt(request);

                    var chatCompletionsOptions = new ChatCompletionsOptions
                    {
                        DeploymentName = _configuration.DeploymentName,
                        Messages =
                        {
                            new ChatRequestSystemMessage("You are an expert text analyst. Analyze the given text and return results in JSON format."),
                            new ChatRequestUserMessage(analysisPrompt)
                        },
                        Temperature = 0.1f,
                        MaxTokens = 1000,
                        ResponseFormat = ChatCompletionsResponseFormat.JsonObject
                    };

                    var response = await _retryPolicy.ExecuteAsync(async () =>
                        await _openAIClient.GetChatCompletionsAsync(
                            chatCompletionsOptions,
                            cancellationToken));

                    var completion = response.Value;
                    var analysisJson = completion.Choices[0].Message.Content;
                    var analysisResult = JsonSerializer.Deserialize<TextAnalysisResponse>(analysisJson);

                    analysisResult!.Model = completion.Model;
                    analysisResult.Usage = new TokenUsage
                    {
                        PromptTokens = completion.Usage.PromptTokens,
                        CompletionTokens = completion.Usage.CompletionTokens,
                        EstimatedCost = _tokenCalculator.CalculateCost(
                            completion.Usage.PromptTokens,
                            completion.Usage.CompletionTokens,
                            completion.Model)
                    };
                    analysisResult.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                    analysisResult.CorrelationId = request.CorrelationId;

                    // Log operation
                    operation.ResponseContent = JsonSerializer.Serialize(analysisResult);
                    operation.PromptTokens = completion.Usage.PromptTokens;
                    operation.CompletionTokens = completion.Usage.CompletionTokens;
                    operation.Cost = analysisResult.Usage.EstimatedCost;
                    operation.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    operation.IsSuccessful = true;

                    await _operationLogger.LogOperationAsync(operation);

                    return analysisResult;
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in text analysis");
                operation.IsSuccessful = false;
                operation.ErrorMessage = ex.Message;
                operation.ErrorCode = ex.GetType().Name;
                operation.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                await _operationLogger.LogOperationAsync(operation);
                throw;
            }
        }

        public async Task<EmbeddingResponse> GenerateEmbeddingAsync(
            EmbeddingRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var operation = new AIOperation
            {
                OperationType = nameof(GenerateEmbeddingAsync),
                UserId = request.UserId,
                Model = request.Model,
                RequestContent = JsonSerializer.Serialize(request)
            };

            try
            {
                await _rateLimitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var embeddingsOptions = new EmbeddingsOptions(request.Model, request.Texts);

                    var response = await _openAIClient.GetEmbeddingsAsync(
                        embeddingsOptions,
                        cancellationToken);

                    var embeddings = response.Value.Data.Select((e, i) => new EmbeddingData
                    {
                        Index = i,
                        Embedding = e.Embedding.ToArray()
                    }).ToArray();

                    var result = new EmbeddingResponse
                    {
                        Embeddings = embeddings,
                        Model = request.Model,
                        Usage = new TokenUsage
                        {
                            PromptTokens = response.Value.Usage.PromptTokens,
                            CompletionTokens = 0,
                            EstimatedCost = _tokenCalculator.CalculateCost(
                                response.Value.Usage.PromptTokens,
                                0,
                                request.Model)
                        },
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        CorrelationId = request.CorrelationId
                    };

                    // Store embeddings in database if document IDs provided
                    if (request.Metadata?.ContainsKey("DocumentIds") == true)
                    {
                        await StoreDocumentEmbeddingsAsync(result, request.Metadata["DocumentIds"] as Guid[]);
                    }

                    // Log operation
                    operation.ResponseContent = $"Generated {embeddings.Length} embeddings";
                    operation.PromptTokens = response.Value.Usage.PromptTokens;
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
                _logger.LogError(ex, "Error generating embeddings");
                operation.IsSuccessful = false;
                operation.ErrorMessage = ex.Message;
                operation.ErrorCode = ex.GetType().Name;
                operation.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                await _operationLogger.LogOperationAsync(operation);
                throw;
            }
        }

        public async Task<SemanticSearchResponse> SemanticSearchAsync(
            SemanticSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Generate embedding for the query
                var queryEmbedding = await GenerateEmbeddingAsync(
                    new EmbeddingRequest
                    {
                        Texts = new[] { request.Query },
                        UserId = request.UserId
                    },
                    cancellationToken);

                // Search for similar documents
                var searchResults = await SearchDocumentsByEmbeddingAsync(
                    queryEmbedding.Embeddings[0].Embedding,
                    request.TopK,
                    request.MinimumScore,
                    request.DocumentIds,
                    request.Filters);

                return new SemanticSearchResponse
                {
                    Results = searchResults.ToArray(),
                    TotalResults = searchResults.Length,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    CorrelationId = request.CorrelationId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in semantic search");
                throw;
            }
        }

        #region Private Helper Methods

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

        private string BuildContentGenerationSystemPrompt(AIContentType contentType)
        {
            return contentType switch
            {
                AIContentType.Technical => "You are a technical content expert. Generate accurate, detailed technical content.",
                AIContentType.Creative => "You are a creative writer. Generate engaging, imaginative content.",
                AIContentType.Business => "You are a business communication expert. Generate professional, clear business content.",
                AIContentType.Academic => "You are an academic writer. Generate well-researched, properly structured academic content.",
                AIContentType.Marketing => "You are a marketing expert. Generate persuasive, engaging marketing content.",
                AIContentType.Code => "You are an expert programmer. Generate clean, efficient, well-documented code.",
                _ => "You are a versatile content creator. Generate high-quality content appropriate to the context."
            };
        }

        private string BuildTextAnalysisPrompt(TextAnalysisRequest request)
        {
            var analysisTypes = string.Join(", ", request.AnalysisTypes.Select(a => a.ToString()));
            return $@"Analyze the following text for {analysisTypes}. 
Return the results in JSON format with appropriate structure for each analysis type.
Text: {request.Text}";
        }

        private async Task<string[]> ExtractKeyPointsAsync(string text, CancellationToken cancellationToken)
        {
            var function = _kernel.CreateFunctionFromPrompt(
                "Extract 3-5 key points from the following text. Return as a JSON array of strings: {{$input}}",
                new OpenAIPromptExecutionSettings { Temperature = 0.1, MaxTokens = 200 });

            var result = await _kernel.InvokeAsync(function, new() { ["input"] = text }, cancellationToken);

            try
            {
                return JsonSerializer.Deserialize<string[]>(result.ToString()) ?? Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private async Task<string[]> GenerateSuggestionsAsync(string content, AIContentType contentType, CancellationToken cancellationToken)
        {
            var function = _kernel.CreateFunctionFromPrompt(
                $"Suggest 2-3 improvements for this {contentType} content. Return as a JSON array of strings: {{{{$input}}}}",
                new OpenAIPromptExecutionSettings { Temperature = 0.3, MaxTokens = 200 });

            var result = await _kernel.InvokeAsync(function, new() { ["input"] = content }, cancellationToken);

            try
            {
                return JsonSerializer.Deserialize<string[]>(result.ToString()) ?? Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private double CalculateConfidenceScore(ChatCompletions completion)
        {
            var choice = completion.Choices[0];
            if (choice.FinishReason == CompletionsFinishReason.Stopped)
                return 0.95;
            else if (choice.FinishReason == CompletionsFinishReason.TokenLimitReached)
                return 0.75;
            else
                return 0.5;
        }

        //private async Task StoreDocumentEmbeddingsAsync(EmbeddingResponse response, int[]? documentIds)
        //{
        //    if (documentIds == null || documentIds.Length == 0) return;

        //    var repository = _unitOfWork.Repository<DocumentEmbedding>();

        //    for (int i = 0; i < Math.Min(response.Embeddings.Length, documentIds.Length); i++)
        //    {
        //        var embedding = new DocumentEmbedding
        //        {
        //            DocumentId = documentIds[i],
        //            Embedding = response.Embeddings[i].Embedding,
        //            Model = response.Model!,
        //            ChunkIndex = i
        //        };

        //        await repository.AddAsync(embedding);
        //    }

        //    await _unitOfWork.CompleteAsync();
        //}


        private async Task StoreDocumentEmbeddingsAsync(EmbeddingResponse response, Guid[]? documentIds)
        {
            if (documentIds == null || documentIds.Length == 0) return;

            var repository = _unitOfWork.Repository<DocumentEmbedding>();

            for (int i = 0; i < Math.Min(response.Embeddings.Length, documentIds.Length); i++)
            {
                var embedding = new DocumentEmbedding
                {
                    DocumentId = documentIds[i],
                    Embedding = response.Embeddings[i].Embedding,
                    Model = response.Model!,
                    ChunkIndex = i
                };

                await repository.AddAsync(embedding);
            }

            await _unitOfWork.CompleteAsync();
        }

        private async Task<SearchResult[]> SearchDocumentsByEmbeddingAsync(
            float[] queryEmbedding,
            int topK,
            double minimumScore,
            string[]? documentIds,
            Dictionary<string, object>? filters)
        {
            var repository = _unitOfWork.Repository<DocumentEmbedding>();
            var allEmbeddings = await repository.GetAllAsync();

            // Filter by document IDs if provided
            if (documentIds?.Length > 0)
            {
                // allEmbeddings = allEmbeddings.Where(e => documentIds.Contains(e.DocumentId.ToString()));
                 allEmbeddings = (await repository.GetAllAsync()).ToList();
            }

            // Calculate cosine similarity and get top results
            var results = allEmbeddings
                .Select(e => new
                {
                    Embedding = e,
                    Score = CalculateCosineSimilarity(queryEmbedding, e.Embedding)
                })
                .Where(r => r.Score >= minimumScore)
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .Select(r => new SearchResult
                {
                    DocumentId = r.Embedding.DocumentId.ToString(),
                    Title = r.Embedding.Document?.Title ?? "Untitled",
                    Content = r.Embedding.ChunkText,
                    Score = r.Score,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ChunkIndex"] = r.Embedding.ChunkIndex,
                        ["Model"] = r.Embedding.Model
                    }
                })
                .ToArray();

            return results;
        }

        private double CalculateCosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0;

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        #endregion
    }
}