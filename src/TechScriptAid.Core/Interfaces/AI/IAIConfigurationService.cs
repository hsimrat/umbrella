using TechScriptAid.Core.DTOs.AI;

namespace TechScriptAid.Core.Interfaces.AI
{
    /// <summary>
    /// Manages AI service configurations
    /// </summary>
    public interface IAIConfigurationService
    {
        /// <summary>
        /// Gets the current AI configuration
        /// </summary>
        Task<AIConfiguration> GetConfigurationAsync();

        /// <summary>
        /// Updates the AI configuration
        /// </summary>
        Task UpdateConfigurationAsync(AIConfiguration configuration);

        /// <summary>
        /// Validates API keys and endpoints
        /// </summary>
        Task<bool> ValidateConfigurationAsync();

        AIConfiguration GetConfiguration();
        string GetEndpoint();
        string GetApiKey();
        string GetDeploymentName();
        RateLimitConfiguration GetRateLimits();
    }

}
