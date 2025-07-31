using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Services;
using System.Diagnostics;
using System.Text.Json;
using TechScriptAid.API.Middleware;
using TechScriptAid.API.Security;
using TechScriptAid.API.Services;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;


namespace TechScriptAid.API.Controllers
{
    /// <summary>
    /// Test controller for demonstrating AI capabilities
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/ai-test")]
    [ApiVersion("1.0")]
    public class AITestController : ControllerBase
    {
        private readonly IAIServiceFactory _aiServiceFactory;
        private readonly IConfiguration _configuration;
        private readonly IOptimizationService _optimizationService;
        private readonly IApiKeyRotationService _keyRotationService;
        private readonly IAIRateLimiter _rateLimiter;
        private readonly ILogger<AITestController> _logger;

        public AITestController(
            IAIServiceFactory aiServiceFactory,
            IConfiguration configuration,
            IOptimizationService optimizationService,
            IApiKeyRotationService keyRotationService,
            IAIRateLimiter rateLimiter,
            ILogger<AITestController> logger)
        {
            _aiServiceFactory = aiServiceFactory;
            _configuration = configuration;
            _optimizationService = optimizationService;
            _keyRotationService = keyRotationService;
            _rateLimiter = rateLimiter;
            _logger = logger;
        }

        /// <summary>
        /// Test both AI providers with the same prompt
        /// </summary>
        [HttpPost("compare-providers")]
        public async Task<IActionResult> CompareProviders([FromBody] CompareProvidersRequest request)
        {
            var results = new Dictionary<string, ProviderTestResult>();
            var providers = new[] { "AzureOpenAI", "OpenAI" };
            var currentProvider = _configuration["AI:Provider"] ?? "AzureOpenAI";


            //foreach (var provider in providers)
            //{
                //    try
                //    {
                //        var stopwatch = Stopwatch.StartNew();
                //        var aiService = _aiServiceFactory.GetService(provider);

                //        var summarizeRequest = new SummarizationRequest
                //        {
                //            Text = request.Text,
                //            MaxSummaryLength = 100,
                //            Style = SummarizationStyle.Paragraph,
                //            UserId = User.Identity?.Name ?? "test-user"
                //        };

                //        var response = await aiService.SummarizeAsync(summarizeRequest);
                //        stopwatch.Stop();

                //        results[provider] = new ProviderTestResult
                //        {
                //            Success = true,
                //            ResponseTime = stopwatch.ElapsedMilliseconds,
                //            TokensUsed = response.Usage?.PromptTokens + response.Usage?.CompletionTokens ?? 0,
                //            Cost = response.Usage?.EstimatedCost ?? 0,
                //            Summary = response.Summary,
                //            Model = response.Model ?? "Unknown"
                //        };
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.LogError(ex, "Provider test failed for {Provider}", provider);
                //        results[provider] = new ProviderTestResult
                //        {
                //            Success = false,
                //            Error = ex.Message
                //        };
                //    }
                //}

                //return Ok(new
                //{
                //    TestId = Guid.NewGuid(),
                //    Timestamp = DateTime.UtcNow,
                //    Results = results,
                //    Recommendation = GetProviderRecommendation(results)
                //});

                // Test the current provider
                try
                {
                    var stopwatch = Stopwatch.StartNew();

                    // Use the current provider (don't specify a different one)
                    var aiService = _aiServiceFactory.GetService(); // Don't pass provider parameter

                    var summarizeRequest = new SummarizationRequest
                    {
                        Text = request.Text,
                        MaxSummaryLength = 100,
                        Style = SummarizationStyle.Paragraph,
                        UserId = User.Identity?.Name ?? "test-user"
                    };

                    var response = await aiService.SummarizeAsync(summarizeRequest);
                    stopwatch.Stop();

                    results[currentProvider] = new ProviderTestResult
                    {
                        Success = true,
                        ResponseTime = stopwatch.ElapsedMilliseconds,
                        TokensUsed = response.Usage?.PromptTokens + response.Usage?.CompletionTokens ?? 0,
                        Cost = response.Usage?.EstimatedCost ?? 0,
                        Summary = response.Summary,
                        Model = response.Model ?? "Unknown"
                    };

                    _logger.LogInformation("Successfully tested {Provider} - Response time: {ResponseTime}ms",
                        currentProvider, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provider test failed for {Provider}", currentProvider);
                    results[currentProvider] = new ProviderTestResult
                    {
                        Success = false,
                        Error = ex.Message
                    };
                }

                // Add info about the other provider
                var otherProvider = currentProvider == "AzureOpenAI" ? "OpenAI" : "AzureOpenAI";
                results[otherProvider] = new ProviderTestResult
                {
                    Success = false,
                    Error = $"Not currently active. To test {otherProvider}, change 'AI:Provider' to '{otherProvider}' in appsettings.json and restart the application."
                };

                return Ok(new
                {
                    TestId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    CurrentProvider = currentProvider,
                    Results = results,
                    Recommendation = GetProviderRecommendation(results),
                    Note = "This endpoint tests the currently configured provider. To compare both providers, you need to run the test twice with different configurations."
                });
            
        }

        /// <summary>
        /// Test rate limiting behavior
        /// </summary>
        [HttpGet("test-rate-limit")]
        public async Task<IActionResult> TestRateLimit()
        {
            var userId = User.Identity?.Name ?? "test-user";
            var results = new List<object>();

            for (int i = 0; i < 10; i++)
            {
                var canProceed = await _rateLimiter.CheckRateLimitAsync(userId);
                var status = await _rateLimiter.GetStatusAsync(userId);

                results.Add(new
                {
                    Attempt = i + 1,
                    Allowed = canProceed,
                    RequestsRemaining = status.RequestsRemaining,
                    ResetsAt = status.ResetsAt
                });

                if (canProceed)
                {
                    await _rateLimiter.RecordRequestAsync(userId, 100);
                }

                await Task.Delay(100); // Small delay between requests
            }

            return Ok(results);
        }

        /// <summary>
        /// Get optimization recommendations
        /// </summary>
        [HttpGet("optimization-recommendations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOptimizationRecommendations()
        {
            var recommendations = await _optimizationService.GetRecommendationsAsync();
            return Ok(recommendations);
        }

        /// <summary>
        /// Test circuit breaker behavior
        /// </summary>
        [HttpPost("test-circuit-breaker")]
        public async Task<IActionResult> TestCircuitBreaker([FromBody] CircuitBreakerTestRequest request)
        {
            var results = new List<CircuitBreakerResult>();  // Use strongly typed list


            for (int i = 0; i < request.NumberOfRequests; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var _aiService = _aiServiceFactory.GetService(); // Don't pass provider parameter

                try
                {
                    if (request.SimulateFailure && i < request.FailuresCount)
                    {
                        // Simulate failure
                        var invalidRequest = new SummarizationRequest
                        {
                            Text = "", // Empty string instead of null
                            MaxSummaryLength = 10, // Valid length
                            UserId = "test-user"
                        };
                        await _aiService.SummarizeAsync(invalidRequest);
                    }
                    else
                    {
                        // Normal request
                        var validRequest = new SummarizationRequest
                        {
                            Text = "This is a test text for circuit breaker pattern demonstration.",
                            MaxSummaryLength = 50,
                            UserId = "test-user"
                        };
                        await _aiService.SummarizeAsync(validRequest);
                    }

                    stopwatch.Stop();
                    results.Add(new CircuitBreakerResult
                    {
                        Attempt = i + 1,
                        Success = true,
                        ResponseTime = stopwatch.ElapsedMilliseconds,
                        CircuitState = "Closed"
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    results.Add(new CircuitBreakerResult
                    {
                        Attempt = i + 1,
                        Success = false,
                        ResponseTime = stopwatch.ElapsedMilliseconds,
                        Error = ex.Message,
                        CircuitState = ex.Message.Contains("circuit") ? "Open" : "Closed"
                    });
                }

                if (request.DelayBetweenRequests > 0)
                {
                    await Task.Delay(request.DelayBetweenRequests);
                }
            }

            // Calculate summary with proper types
            var summary = new
            {
                TotalRequests = results.Count,
                SuccessfulRequests = results.Count(r => r.Success),
                FailedRequests = results.Count(r => !r.Success),
                AverageResponseTime = results.Any() ? results.Average(r => r.ResponseTime) : 0,
                MaxResponseTime = results.Any() ? results.Max(r => r.ResponseTime) : 0,
                MinResponseTime = results.Any() ? results.Min(r => r.ResponseTime) : 0
            };

            return Ok(new
            {
                TestConfiguration = request,
                Results = results,
                Summary = summary,
                CircuitBreakerTriggered = results.Any(r => r.CircuitState == "Open"),
                Timestamp = DateTime.UtcNow
            });
        }

        // Add this class inside the controller or in a separate file
        public class CircuitBreakerResult
        {
            public int Attempt { get; set; }
            public bool Success { get; set; }
            public long ResponseTime { get; set; }
            public string? Error { get; set; }
            public string CircuitState { get; set; } = "Closed";
        }

        /// <summary>
        /// Get current metrics
        /// </summary>
        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            var metrics = MetricsMiddleware.GetMetrics();
            return Ok(metrics);
        }

        /// <summary>
        /// Test API key rotation
        /// </summary>
        [HttpPost("rotate-api-key/{provider}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RotateApiKey(string provider)
        {
            var success = await _keyRotationService.RotateApiKeyAsync(provider);
            var nextRotation = await _keyRotationService.GetNextRotationDateAsync(provider);

            return Ok(new
            {
                Provider = provider,
                Success = success,
                NextRotationDate = nextRotation,
                Message = success
                    ? "API key rotated successfully"
                    : "API key rotation failed. Check logs for details."
            });
        }

        /// <summary>
        /// Demonstrate all patterns in action
        /// </summary>
        [HttpPost("demo-all-patterns")]
        public async Task<IActionResult> DemoAllPatterns([FromBody] string text)
        {
            var results = new
            {
                Patterns = new Dictionary<string, object>(),
                Timeline = new List<object>()
            };

            var stopwatch = Stopwatch.StartNew();

            // 1. Factory Pattern
            results.Timeline.Add(new { Time = stopwatch.ElapsedMilliseconds, Event = "Getting AI Service via Factory" });
            var currentProvider = _configuration["AI:Provider"];
            var aiService = _aiServiceFactory.GetService();
            results.Patterns["FactoryPattern"] = new { CurrentProvider = currentProvider };

            // 2. Rate Limiting
            results.Timeline.Add(new { Time = stopwatch.ElapsedMilliseconds, Event = "Checking Rate Limits" });
            var userId = "demo-user";
            var canProceed = await _rateLimiter.CheckRateLimitAsync(userId);
            var rateLimitStatus = await _rateLimiter.GetStatusAsync(userId);
            results.Patterns["RateLimiting"] = new
            {
                Allowed = canProceed,
                RequestsRemaining = rateLimitStatus.RequestsRemaining,
                ResetsAt = rateLimitStatus.ResetsAt
            };

            if (!canProceed)
            {
                return StatusCode(429, results);
            }

            // 3. Caching (First request will miss, second will hit)
            results.Timeline.Add(new { Time = stopwatch.ElapsedMilliseconds, Event = "First Request (Cache Miss Expected)" });
            var request = new SummarizationRequest
            {
                Text = text,
                MaxSummaryLength = 100,
                UserId = userId
            };

            var response1 = await aiService.SummarizeAsync(request);
            var time1 = stopwatch.ElapsedMilliseconds;

            results.Timeline.Add(new { Time = stopwatch.ElapsedMilliseconds, Event = "Second Request (Cache Hit Expected)" });
            var response2 = await aiService.SummarizeAsync(request);
            var time2 = stopwatch.ElapsedMilliseconds - time1;

            results.Patterns["Caching"] = new
            {
                FirstRequestTime = time1,
                SecondRequestTime = time2,
                CacheHit = time2 < time1 / 2 // If second request is less than half the time, likely a cache hit
            };

            // 4. Retry & Circuit Breaker (demonstrated in logs)
            results.Patterns["RetryPolicy"] = new
            {
                MaxRetries = _configuration.GetValue<int>("AI:MaxRetries"),
                BackoffStrategy = "Exponential"
            };

            results.Patterns["CircuitBreaker"] = new
            {
                FailureThreshold = 5,
                BreakDuration = "30 seconds",
                CurrentState = "Closed" // Would be "Open" if breaker is tripped
            };

            // 5. Token Tracking
            await _rateLimiter.RecordRequestAsync(userId, response1.Usage?.PromptTokens + response1.Usage?.CompletionTokens ?? 0);
            results.Patterns["TokenTracking"] = new
            {
                TokensUsed = response1.Usage,
                EstimatedCost = response1.Usage?.EstimatedCost
            };

            // 6. Health Checks
            results.Timeline.Add(new { Time = stopwatch.ElapsedMilliseconds, Event = "Health Check Status" });
            results.Patterns["HealthChecks"] = new
            {
                Endpoints = new[]
                {
                    "/health - Overall system health",
                    "/health/ready - Readiness probe",
                    "/health/live - Liveness probe",
                    "/health/dashboard - Visual dashboard"
                }
            };

            stopwatch.Stop();
            results.Timeline.Add(new { Time = stopwatch.ElapsedMilliseconds, Event = "Demo Complete" });

            return Ok(results);
        }

        private string GetProviderRecommendation(Dictionary<string, ProviderTestResult> results)
        {
            var successful = results.Where(r => r.Value.Success).ToList();
            if (!successful.Any())
                return "Both providers failed. Check configuration and API keys.";

            var fastest = successful.OrderBy(r => r.Value.ResponseTime).First();
            var cheapest = successful.OrderBy(r => r.Value.Cost).First();

            if (fastest.Key == cheapest.Key)
                return $"{fastest.Key} is recommended (fastest and most cost-effective)";

            return $"For speed: use {fastest.Key} ({fastest.Value.ResponseTime}ms). " +
                   $"For cost: use {cheapest.Key} (${cheapest.Value.Cost:F4})";
        }

        // Add this method to your existing AITestController class:

        [HttpGet("cache-info")]
        public IActionResult GetCacheInfo([FromServices] IAICacheService cacheService)
        {
            var cacheType = cacheService.GetType().Name;

            // Get additional info
            var additionalInfo = new Dictionary<string, object>
            {
                ["CacheImplementation"] = cacheType,
                ["IsRedis"] = cacheType == "RedisCacheService",
                ["IsMemory"] = cacheType == "MemoryCacheService",
                ["Timestamp"] = DateTime.UtcNow
            };

            // If it's Redis, try to get connection info
            if (cacheType == "RedisCacheService")
            {
                additionalInfo["RedisConnection"] = _configuration.GetConnectionString("Redis") ?? "Not configured";
            }

            return Ok(additionalInfo);
        }

        // Also add this method to test cache functionality:
        [HttpPost("test-cache")]
        public async Task<IActionResult> TestCache([FromBody] string testValue)
        {
            var cacheService = HttpContext.RequestServices.GetRequiredService<IAICacheService>();
            var key = $"test-cache-{Guid.NewGuid():N}";

            try
            {
                // Store in cache
                var testData = new
                {
                    Value = testValue,
                    StoredAt = DateTime.UtcNow,
                    CacheType = cacheService.GetType().Name
                };

                await cacheService.SetAsync(key, testData, TimeSpan.FromMinutes(1));

                // Retrieve from cache
                var retrieved = await cacheService.GetAsync<dynamic>(key);

                return Ok(new
                {
                    Success = true,
                    CacheType = cacheService.GetType().Name,
                    Key = key,
                    StoredData = testData,
                    RetrievedData = retrieved,
                    // DataMatches = retrieved != null
                    DataMatches = retrieved is not null && retrieved.ValueKind != JsonValueKind.Null && retrieved.ValueKind != JsonValueKind.Undefined
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    CacheType = cacheService.GetType().Name,
                    Error = ex.Message
                });
            }
        }
    }

    public class CompareProvidersRequest
    {
        public string Text { get; set; } = "The artificial intelligence revolution is transforming how we work, communicate, and solve complex problems. Machine learning algorithms can now process vast amounts of data, identify patterns, and make predictions with remarkable accuracy.";
    }

    public class ProviderTestResult
    {
        public bool Success { get; set; }
        public long ResponseTime { get; set; }
        public int TokensUsed { get; set; }
        public decimal Cost { get; set; }
        public string? Summary { get; set; }
        public string? Error { get; set; }
        public string Model { get; set; } = string.Empty;
    }

    public class CircuitBreakerTestRequest
    {
        public int NumberOfRequests { get; set; } = 10;
        public bool SimulateFailure { get; set; } = true;
        public int FailuresCount { get; set; } = 6;
        public int DelayBetweenRequests { get; set; } = 100;
    }
}