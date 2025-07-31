using System.Security.Cryptography;
using System.Text.Json;
using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.API.Security
{
    public interface IApiKeyRotationService
    {
        Task<bool> RotateApiKeyAsync(string provider);
        Task<DateTime> GetNextRotationDateAsync(string provider);
    }

    public class ApiKeyRotationService : IApiKeyRotationService
    {
        private readonly ISecureConfigurationService _secureConfig;
        private readonly ILogger<ApiKeyRotationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ApiKeyRotationService(
            ISecureConfigurationService secureConfig,
            ILogger<ApiKeyRotationService> logger,
            IServiceProvider serviceProvider)
        {
            _secureConfig = secureConfig;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<bool> RotateApiKeyAsync(string provider)
        {
            try
            {
                _logger.LogInformation("Starting API key rotation for provider: {Provider}", provider);

                // In a real implementation, this would:
                // 1. Generate new API key with the provider
                // 2. Test the new key
                // 3. Update configuration
                // 4. Invalidate old key

                // For demo purposes, we'll simulate the rotation
                var newKey = GenerateNewApiKey();
                _secureConfig.SetSecureValue($"AI:{provider}:ApiKey", newKey);

                // Test the new configuration
                using var scope = _serviceProvider.CreateScope();
                var configService = scope.ServiceProvider.GetRequiredService<IAIConfigurationService>();
                var isValid = await configService.ValidateConfigurationAsync();

                if (isValid)
                {
                    _logger.LogInformation("API key rotation completed successfully for provider: {Provider}", provider);

                    // Log rotation event
                    await LogRotationEventAsync(provider, true);

                    return true;
                }
                else
                {
                    _logger.LogError("API key rotation failed validation for provider: {Provider}", provider);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API key rotation failed for provider: {Provider}", provider);
                await LogRotationEventAsync(provider, false);
                return false;
            }
        }

        public async Task<DateTime> GetNextRotationDateAsync(string provider)
        {
            // In production, this would check the last rotation date from a database
            // For demo, return 90 days from now
            return await Task.FromResult(DateTime.UtcNow.AddDays(90));
        }

        private string GenerateNewApiKey()
        {
            // This is a simulation. In production, you would request a new key from the provider
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return $"sk-demo-{Convert.ToBase64String(bytes).Replace("/", "-").Replace("+", "_").Substring(0, 48)}";
        }

        private async Task LogRotationEventAsync(string provider, bool success)
        {
            // Log to your audit system
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Provider = provider,
                Success = success,
                User = "System",
                Action = "API_KEY_ROTATION"
            };

            _logger.LogInformation("API Key Rotation Event: {LogEntry}", JsonSerializer.Serialize(logEntry));
            await Task.CompletedTask;
        }
    }
}