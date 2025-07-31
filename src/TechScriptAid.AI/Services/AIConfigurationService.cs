using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;


namespace TechScriptAid.AI.Services
{
    public class AIConfigurationService : IAIConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIConfigurationService> _logger;
        private AIConfiguration? _cachedConfiguration;
        private DateTime _lastConfigurationLoad = DateTime.MinValue;
        private readonly TimeSpan _configurationCacheDuration = TimeSpan.FromMinutes(5);

        public AIConfigurationService(
            IConfiguration configuration,
            ILogger<AIConfigurationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AIConfiguration> GetConfigurationAsync()
        {
            if (_cachedConfiguration != null &&
                DateTime.UtcNow - _lastConfigurationLoad < _configurationCacheDuration)
            {
                return await Task.FromResult(_cachedConfiguration);
            }

            var config = new AIConfiguration
            {
                Provider = _configuration["AI:Provider"] ?? "AzureOpenAI",
                Endpoint = _configuration["AI:AzureOpenAI:Endpoint"] ?? "",
                ApiKey = _configuration["AI:AzureOpenAI:ApiKey"] ?? "",
                DeploymentName = _configuration["AI:AzureOpenAI:DeploymentName"] ?? "",
                ApiVersion = _configuration["AI:AzureOpenAI:ApiVersion"] ?? "2024-02-01",
                MaxRetries = int.Parse(_configuration["AI:MaxRetries"] ?? "3"),
                TimeoutSeconds = int.Parse(_configuration["AI:TimeoutSeconds"] ?? "30"),
                RateLimits = new RateLimitConfiguration
                {
                    RequestsPerMinute = int.Parse(_configuration["AI:RateLimits:RequestsPerMinute"] ?? "60"),
                    TokensPerMinute = int.Parse(_configuration["AI:RateLimits:TokensPerMinute"] ?? "90000"),
                    ConcurrentRequests = int.Parse(_configuration["AI:RateLimits:ConcurrentRequests"] ?? "10")
                }
            };

            _cachedConfiguration = config;
            _lastConfigurationLoad = DateTime.UtcNow;

            _logger.LogInformation("AI configuration loaded successfully");
            return await Task.FromResult(config);
        }

        public async Task UpdateConfigurationAsync(AIConfiguration configuration)
        {
            _cachedConfiguration = configuration;
            _lastConfigurationLoad = DateTime.UtcNow;
            _logger.LogInformation("AI configuration updated");
            await Task.CompletedTask;
        }

        public async Task<bool> ValidateConfigurationAsync()
        {
            try
            {
                var config = await GetConfigurationAsync();

                // Basic validation without making API calls
                bool isValid = !string.IsNullOrEmpty(config.Endpoint) &&
                              !string.IsNullOrEmpty(config.ApiKey) &&
                              !string.IsNullOrEmpty(config.DeploymentName);

                _logger.LogInformation("AI configuration validated: {IsValid}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI configuration validation failed");
                return false;
            }
        }

        public AIConfiguration GetConfiguration()
        {
            if (_cachedConfiguration == null)
            {
                throw new InvalidOperationException("Configuration has not been loaded yet.");
            }
            return _cachedConfiguration;
        }

        public string GetEndpoint()
        {
            return _configuration["AI:AzureOpenAI:Endpoint"] ?? "";
        }

        public string GetApiKey()
        {
            return _configuration["AI:AzureOpenAI:ApiKey"] ?? "";
        }

        public string GetDeploymentName()
        {
            return _configuration["AI:AzureOpenAI:DeploymentName"] ?? "gpt-35-turbo";
        }

        public RateLimitConfiguration GetRateLimits()
        {
            return new RateLimitConfiguration
            {
                RequestsPerMinute = int.Parse(_configuration["AI:RateLimits:RequestsPerMinute"] ?? "60"),
                TokensPerMinute = int.Parse(_configuration["AI:RateLimits:TokensPerMinute"] ?? "90000"),
                ConcurrentRequests = int.Parse(_configuration["AI:RateLimits:ConcurrentRequests"] ?? "10")
            };
        }

        private void ValidateConfiguration()
        {
            var endpoint = _configuration["AI:AzureOpenAI:Endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                _logger.LogError("Azure OpenAI Endpoint is not configured");
                throw new InvalidOperationException("Azure OpenAI Endpoint is required");
            }

            var apiKey = _configuration["AI:AzureOpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Azure OpenAI API Key is not configured");
                throw new InvalidOperationException("Azure OpenAI API Key is required");
            }

            var deploymentName = _configuration["AI:AzureOpenAI:DeploymentName"];
            if (string.IsNullOrWhiteSpace(deploymentName))
            {
                _logger.LogWarning("Deployment name not specified, using default: gpt-35-turbo");
                deploymentName = "gpt-35-turbo";
            }

            if (_configuration["AI:RateLimits:RequestsPerMinute"] == null ||
                _configuration["AI:RateLimits:TokensPerMinute"] == null ||
                _configuration["AI:RateLimits:ConcurrentRequests"] == null)
            {
                _logger.LogWarning("Rate limits not configured, using defaults");
            }

            _logger.LogInformation("AI Configuration validated successfully");
        }
    }
}