using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using TechScriptAid.Core.DTOs.AI;

namespace TechScriptAid.API.Security
{
    public static class SecureConfiguration
    {
        public static IConfigurationBuilder AddSecureConfiguration(
            this IConfigurationBuilder builder,
            IWebHostEnvironment environment)
        {
            //    if (environment.IsProduction())
            //    {
            //        var keyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
            //        if (!string.IsNullOrEmpty(keyVaultName))
            //        {
            //            var keyVaultClient = new KeyVaultClient(
            //                new KeyVaultClient.AuthenticationCallback(
            //                    new AzureServiceTokenProvider().KeyVaultTokenCallback));

            //            builder.AddAzureKeyVault(
            //                $"https://{keyVaultName}.vault.azure.net/",
            //                keyVaultClient,
            //                new DefaultKeyVaultSecretManager());
            //        }
            //    }
            //    else
            //    {
            //        // Use local secrets in development
            //        builder.AddUserSecrets<Program>();
            //    }

            //    return builder;
            //}

            builder.AddUserSecrets<Program>();

            // TODO: Add Azure Key Vault for production
            // if (environment.IsProduction())
            // {
            //     // Add Key Vault configuration here
            // }

            return builder;


            //            Option 2: Use Simple Environment Variables:
            //csharppublic static IConfigurationBuilder AddSecureConfiguration(
            //    this IConfigurationBuilder builder,
            //    IWebHostEnvironment environment)
            //            {
            //                // Add environment variables (works everywhere)
            //                builder.AddEnvironmentVariables();

            //                // Add user secrets for local development
            //                if (environment.IsDevelopment())
            //                {
            //                    builder.AddUserSecrets<Program>();
            //                }

            //                return builder;
            //            }
        }
    }
    public interface ISecureConfigurationService
    {
        string GetSecureValue(string key);
        void SetSecureValue(string key, string value);
    }

    public class SecureConfigurationService : ISecureConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IDataProtector _protector;
        private readonly ILogger<SecureConfigurationService> _logger;

        public SecureConfigurationService(
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<SecureConfigurationService> logger)
        {
            _configuration = configuration;
            _protector = dataProtectionProvider.CreateProtector("TechScriptAid.AI.ApiKeys");
            _logger = logger;
        }

        public string GetSecureValue(string key)
        {
            var encryptedValue = _configuration[key];
            if (string.IsNullOrEmpty(encryptedValue))
                return string.Empty;

            try
            {
                // If value starts with "encrypted:", decrypt it
                if (encryptedValue.StartsWith("encrypted:"))
                {
                    var encrypted = encryptedValue.Substring("encrypted:".Length);
                    return _protector.Unprotect(encrypted);
                }

                // Otherwise, return as-is (for backward compatibility)
                return encryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt configuration value for key: {Key}", key);
                throw new InvalidOperationException($"Failed to decrypt configuration value for key: {key}");
            }
        }

        public void SetSecureValue(string key, string value)
        {
            var encrypted = _protector.Protect(value);
            _configuration[key] = $"encrypted:{encrypted}";
        }
    }

    // Extension for secure AI configuration
    public static class SecureAIConfigurationExtensions
    {
        public static IServiceCollection AddSecureAIConfiguration(
            this IServiceCollection services)
        {
            services.AddDataProtection()
                .SetApplicationName("TechScriptAid")
                .PersistKeysToFileSystem(new DirectoryInfo(@"./keys"));

            services.AddSingleton<ISecureConfigurationService, SecureConfigurationService>();

            // Override AI configuration to use secure values
            services.PostConfigure<AIConfiguration>(options =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var secureConfig = scope.ServiceProvider.GetRequiredService<ISecureConfigurationService>();

                if (!string.IsNullOrEmpty(options.ApiKey))
                {
                    options.ApiKey = secureConfig.GetSecureValue($"AI:{options.Provider}:ApiKey");
                }
            });

            return services;
        }
    }
}