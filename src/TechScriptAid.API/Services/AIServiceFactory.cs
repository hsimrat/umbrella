using Microsoft.Extensions.DependencyInjection;
using TechScriptAid.Core.Interfaces.AI;
using TechScriptAid.AI.Services;

namespace TechScriptAid.API.Services
{
    public interface IAIServiceFactory
    {
        IAIService GetService(string? provider = null);
        Task<IAIService> GetServiceAsync(string? provider = null);
    }

    public class AIServiceFactory : IAIServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIServiceFactory> _logger;

        public AIServiceFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AIServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public IAIService GetService(string? provider = null)
        {
            provider ??= _configuration["AI:Provider"] ?? "AzureOpenAI";
            _logger.LogInformation("Creating AI service for provider: {Provider}", provider);

            // Since we can only have one IAIService registered at a time,
            // just return the registered IAIService
            // The actual provider is determined by configuration at startup

            var currentProvider = _configuration["AI:Provider"] ?? "OpenAI";

            if (!provider.Equals(currentProvider, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    $"Cannot switch to provider '{provider}'. Current provider is '{currentProvider}'. " +
                    "To change providers, update appsettings.json and restart the application.");
            }

            //  return _serviceProvider.GetRequiredService<IAIService>();

            return provider.ToLower() switch
            {
                "azureopenai" => _serviceProvider.GetRequiredService<AzureOpenAIService>(),
                "openai" => _serviceProvider.GetRequiredService<OpenAIService>(),
                _ => throw new NotSupportedException($"AI provider '{provider}' is not supported")
            };
        }

        public async Task<IAIService> GetServiceAsync(string? provider = null)
        {
            // Can be used for async initialization if needed in the future
            return await Task.FromResult(GetService(provider));
        }
    }
}