using Microsoft.Extensions.Diagnostics.HealthChecks;
using TechScriptAid.Core.Interfaces.AI;
using TechScriptAid.API.Services;
using TechScriptAid.Core.DTOs.AI;

namespace TechScriptAid.API.HealthChecks
{
    public class AIProviderHealthCheck : IHealthCheck
    {
        private readonly IAIServiceFactory _aiServiceFactory;
        private readonly ILogger<AIProviderHealthCheck> _logger;
        private readonly IConfiguration _configuration;

        public AIProviderHealthCheck(
            IAIServiceFactory aiServiceFactory,
            ILogger<AIProviderHealthCheck> logger,
            IConfiguration configuration)
        {
            _aiServiceFactory = aiServiceFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var provider = _configuration["AI:Provider"] ?? "Unknown";
            var data = new Dictionary<string, object> { ["provider"] = provider };

            try
            {
                var aiService = _aiServiceFactory.GetService();

                // Simple connectivity test using embeddings (cheaper than chat)
                var testRequest = new EmbeddingRequest
                {
                    Texts = new[] { "health check test" },
                    UserId = "system-health-check",
                    Model = provider == "OpenAI"
                        ? "text-embedding-ada-002"
                        : _configuration["AI:AzureOpenAI:DeploymentName"] + "-embedding"
                };

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var result = await aiService.GenerateEmbeddingAsync(testRequest, cts.Token);

                if (result?.Embeddings?.Any() == true)
                {
                    data["responseTime"] = result.ProcessingTimeMs;
                    data["model"] = result.Model ?? "unknown";

                    return HealthCheckResult.Healthy(
                        $"AI provider {provider} is responsive",
                        data);
                }

                return HealthCheckResult.Degraded(
                    $"AI provider {provider} returned empty response",
                    null, // Fix: Pass null for the exception parameter
                    data);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("AI provider health check timed out");
                return HealthCheckResult.Degraded(
                    $"AI provider {provider} health check timed out",
                    null, // Fix: Pass null for the exception parameter
                    data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI provider health check failed");

                data.Add("error", ex.Message);
                data.Add("type", ex.GetType().Name);

                return HealthCheckResult.Unhealthy(
                    $"AI provider {provider} is not accessible",
                    ex,
                    data);
            }
        }
    }
}