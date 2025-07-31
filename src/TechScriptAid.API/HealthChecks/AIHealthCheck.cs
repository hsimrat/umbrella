using Microsoft.Extensions.Diagnostics.HealthChecks;
using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.API.HealthChecks
{
    /// <summary>
    /// Health check for AI services
    /// </summary>
    public class AIHealthCheck : IHealthCheck
    {
        private readonly IAIConfigurationService _configurationService;
        private readonly ILogger<AIHealthCheck> _logger;

        public AIHealthCheck(
            IAIConfigurationService configurationService,
            ILogger<AIHealthCheck> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isValid = await _configurationService.ValidateConfigurationAsync();

                if (isValid)
                {
                    return HealthCheckResult.Healthy("AI service is healthy");
                }

                return HealthCheckResult.Unhealthy("AI service configuration is invalid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI health check failed");
                return HealthCheckResult.Unhealthy("AI service health check failed", ex);
            }
        }
    }
}
