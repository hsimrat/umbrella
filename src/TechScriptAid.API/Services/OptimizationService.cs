using System.Collections.Concurrent;
using System.Text.Json;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.API.Services
{
    public interface IOptimizationService
    {
        Task<OptimizationRecommendations> GetRecommendationsAsync();
        Task ApplyOptimizationsAsync(OptimizationSettings settings);
    }

    public class OptimizationService : IOptimizationService
    {
        private readonly ILogger<OptimizationService> _logger;
        private readonly IAIOperationLogger _operationLogger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new();

        public OptimizationService(
            ILogger<OptimizationService> logger,
            IAIOperationLogger operationLogger,
            IConfiguration configuration)
        {
            _logger = logger;
            _operationLogger = operationLogger;
            _configuration = configuration;
        }

        public async Task<OptimizationRecommendations> GetRecommendationsAsync()
        {
            var operations = await _operationLogger.GetOperationHistoryAsync(1, 1000);
            var recommendations = new OptimizationRecommendations();

            // Analyze retry patterns
            var failureRate = operations.Count(o => !o.IsSuccessful) / (double)operations.Count();
            if (failureRate > 0.1) // More than 10% failures
            {
                recommendations.RetryPolicy = new RetryPolicyRecommendation
                {
                    CurrentRetries = _configuration.GetValue<int>("AI:MaxRetries"),
                    RecommendedRetries = 5,
                    CurrentBackoff = "Exponential",
                    RecommendedBackoff = "Exponential with jitter",
                    Reason = $"High failure rate detected: {failureRate:P}"
                };
            }

            // Analyze cache performance
            var cacheableOps = operations.Where(o =>
                o.OperationType == "SummarizeAsync" ||
                o.OperationType == "GenerateContentAsync");

            var avgResponseTime = cacheableOps.Average(o => o.ResponseTimeMs);
            if (avgResponseTime > 1000) // More than 1 second average
            {
                recommendations.CacheStrategy = new CacheRecommendation
                {
                    CurrentTTL = TimeSpan.FromHours(1),
                    RecommendedTTL = TimeSpan.FromHours(4),
                    AdditionalKeys = new[] { "user_context", "model_version" },
                    Reason = $"High average response time: {avgResponseTime}ms"
                };
            }

            // Token usage optimization
            var avgTokens = operations.Average(o => o.TotalTokens);
            if (avgTokens > 1000)
            {
                recommendations.TokenOptimization = new TokenOptimizationRecommendation
                {
                    CurrentAvgTokens = (int)avgTokens,
                    RecommendedMaxTokens = 800,
                    ModelSuggestion = "Consider using gpt-3.5-turbo for simpler tasks",
                    Reason = "High token usage detected"
                };
            }

            return recommendations;
        }

        public async Task ApplyOptimizationsAsync(OptimizationSettings settings)
        {
            _logger.LogInformation("Applying optimization settings: {Settings}",
                JsonSerializer.Serialize(settings));

            // Update configuration dynamically
            if (settings.UpdateRetryPolicy)
            {
                _configuration["AI:MaxRetries"] = settings.MaxRetries.ToString();
                _configuration["AI:RetryBackoff"] = settings.RetryBackoff;
            }

            if (settings.UpdateCacheSettings)
            {
                _configuration["AI:Cache:SummarizationTTL"] = settings.CacheTTL.TotalMinutes.ToString();
                _configuration["AI:Cache:ContentGenerationTTL"] = settings.CacheTTL.TotalMinutes.ToString();
            }

            if (settings.UpdateRateLimits)
            {
                _configuration["AI:RateLimits:RequestsPerMinute"] = settings.RequestsPerMinute.ToString();
                _configuration["AI:RateLimits:TokensPerMinute"] = settings.TokensPerMinute.ToString();
            }

            await Task.CompletedTask;
        }
    }

    public class OptimizationRecommendations
    {
        public RetryPolicyRecommendation? RetryPolicy { get; set; }
        public CacheRecommendation? CacheStrategy { get; set; }
        public TokenOptimizationRecommendation? TokenOptimization { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class RetryPolicyRecommendation
    {
        public int CurrentRetries { get; set; }
        public int RecommendedRetries { get; set; }
        public string CurrentBackoff { get; set; } = string.Empty;
        public string RecommendedBackoff { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class CacheRecommendation
    {
        public TimeSpan CurrentTTL { get; set; }
        public TimeSpan RecommendedTTL { get; set; }
        public string[] AdditionalKeys { get; set; } = Array.Empty<string>();
        public string Reason { get; set; } = string.Empty;
    }

    public class TokenOptimizationRecommendation
    {
        public int CurrentAvgTokens { get; set; }
        public int RecommendedMaxTokens { get; set; }
        public string ModelSuggestion { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class OptimizationSettings
    {
        public bool UpdateRetryPolicy { get; set; }
        public int MaxRetries { get; set; }
        public string RetryBackoff { get; set; } = "Exponential";

        public bool UpdateCacheSettings { get; set; }
        public TimeSpan CacheTTL { get; set; }

        public bool UpdateRateLimits { get; set; }
        public int RequestsPerMinute { get; set; }
        public int TokensPerMinute { get; set; }
    }

    public class PerformanceMetric
    {
        public string Operation { get; set; } = string.Empty;
        public double AverageResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public int TotalRequests { get; set; }
    }
}