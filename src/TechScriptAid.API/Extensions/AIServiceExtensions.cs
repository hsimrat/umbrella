using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using TechScriptAid.AI.Services;
using TechScriptAid.API.HealthChecks;
using TechScriptAid.API.Services;
using TechScriptAid.Core.Interfaces.AI;

using AIConfiguration = TechScriptAid.Core.DTOs.AI.AIConfiguration;

namespace TechScriptAid.API.Extensions
{
    /// <summary>
    /// Extension methods for configuring AI services
    /// </summary>
    public static class AIServiceExtensions
    {
        public static IServiceCollection AddAIServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            //// Configure AI settings
            //var aiConfigSection = configuration.GetSection("AI");
            //var provider = aiConfigSection["Provider"] ?? "AzureOpenAI";

            //// Create AIConfiguration from your nested structure
            // var aiConfig = new AIConfiguration();
            //{
            //    Provider = provider,
            //    Endpoint = string.Empty,
            //    ApiKey = string.Empty,
            //    DeploymentName = string.Empty,
            //    ApiVersion = "2024-02-01",
            //    MaxRetries = aiConfigSection.GetValue<int>("MaxRetries", 3),
            //    TimeoutSeconds = aiConfigSection.GetValue<int>("TimeoutSeconds", 30),
            //    RateLimits = new RateLimitConfiguration
            //    {
            //        ConcurrentRequests = aiConfigSection.GetValue<int>("RateLimits:ConcurrentRequests", 10),
            //        RequestsPerMinute = aiConfigSection.GetValue<int>("RateLimits:RequestsPerMinute", 60),
            //        RequestsPerHour = aiConfigSection.GetValue<int>("RateLimits:RequestsPerHour", 3600),
            //        TokensPerMinute = aiConfigSection.GetValue<int>("RateLimits:TokensPerMinute", 90000)
            //    }
            //};

            //// Configure provider-specific settings
            //if (provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
            //{
            //    var azureSection = aiConfigSection.GetSection("AzureOpenAI");
            //    aiConfig.Endpoint = azureSection["Endpoint"] ?? throw new InvalidOperationException("AI:AzureOpenAI:Endpoint is required");
            //    aiConfig.ApiKey = azureSection["ApiKey"] ?? throw new InvalidOperationException("AI:AzureOpenAI:ApiKey is required");
            //    aiConfig.DeploymentName = azureSection["DeploymentName"] ?? "gpt-4";
            //    aiConfig.ApiVersion = azureSection["ApiVersion"] ?? "2024-02-01";

            //    // Register Azure OpenAI specific services
            //    RegisterAzureOpenAIServices(services, aiConfig);
            //}
            //else if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            //{
            //    var openAISection = aiConfigSection.GetSection("OpenAI");
            //    aiConfig.ApiKey = openAISection["ApiKey"] ?? throw new InvalidOperationException("AI:OpenAI:ApiKey is required");
            //    aiConfig.DeploymentName = openAISection["Model"] ?? "gpt-3.5-turbo";

            //    // Register OpenAI specific services
            //    RegisterOpenAIServices(services, aiConfig, configuration);
            //}

            //// Register the configuration
            //services.Configure<AIConfiguration>(options =>
            //{
            //    options.Provider = aiConfig.Provider;
            //    options.Endpoint = aiConfig.Endpoint;
            //    options.ApiKey = aiConfig.ApiKey;
            //    options.DeploymentName = aiConfig.DeploymentName;
            //    options.ApiVersion = aiConfig.ApiVersion;
            //    options.MaxRetries = aiConfig.MaxRetries;
            //    options.TimeoutSeconds = aiConfig.TimeoutSeconds;
            //    options.RateLimits = aiConfig.RateLimits;
            //});

            //// Register AI Service Factory
            //services.AddSingleton<IAIServiceFactory, AIServiceFactory>();

            //// Register core AI services
            //services.AddScoped<IAIOperationLogger, AIOperationLogger>();
            //services.AddScoped<IAIConfigurationService, AIConfigurationService>();
            //services.AddSingleton<ITokenCalculator, TokenCalculator>();
            //services.AddSingleton<IAIRateLimiter, AIRateLimiter>();
            //services.AddScoped<SemanticKernelService>();


            Console.WriteLine("[AI SERVICES] Starting AI services configuration...");
            //// Configure caching with proper services
            ConfigureCaching(services, configuration);

            Console.WriteLine("[AI SERVICES] AI services configuration completed.");

            //// Configure rate limiting
            //ConfigureRateLimiting(services, aiConfig);

            //// Add health checks
            //ConfigureHealthChecks(services, configuration);

            //return services;

            try
            {

                var aiConfigSection = configuration.GetSection("AI");
                var provider = aiConfigSection["Provider"] ?? "AzureOpenAI";

                Console.WriteLine($"[DEBUG] Configuring AI services for provider: {provider}");

                // Your existing configuration code...
                var aiConfig = new AIConfiguration
                {
                    Provider = provider,
                    // ... rest of configuration
                };
                // Check what provider we're using
                if (provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[DEBUG] Registering Azure OpenAI services");
                    RegisterAzureOpenAIServices(services, aiConfig);
                }
                else if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[DEBUG] Registering OpenAI services");
                    RegisterOpenAIServices(services, aiConfig, configuration);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown AI provider: {provider}");
                }

                // Temporarily add just the basic services to isolate the issue
                services.AddSingleton<ITokenCalculator, TokenCalculator>();
                services.AddScoped<IAIOperationLogger, AIOperationLogger>();
                services.AddScoped<IAIConfigurationService, AIConfigurationService>();

                services.AddSingleton<IAIRateLimiter, AIRateLimiter>();
                services.AddScoped<SemanticKernelService>();
                // services.AddSingleton<IAIServiceFactory, AIServiceFactory>();
                services.AddScoped<IAIServiceFactory, AIServiceFactory>();

                // Add memory cache as default
                services.AddMemoryCache();
                services.AddScoped<IAICacheService, MemoryCacheService>();

                // Add a simple version first
              //  var provider = configuration["AI:Provider"] ?? "OpenAI";
                if (provider == "OpenAI")
                {
                    services.AddHttpClient<OpenAIService>();
                    services.AddScoped<IAIService, OpenAIService>();
                }

                // Add health checks
                services.AddHealthChecks()
                    .AddCheck<AIHealthCheck>("ai_service");

                ConfigureRateLimiting(services, aiConfig);


                // Add this at the end to verify registration
                Console.WriteLine($"[DEBUG] Total services registered: {services.Count}");
                var aiService = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));
                Console.WriteLine($"[DEBUG] IAIService registered: {aiService != null}");

                return services;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to configure AI services: {ex.Message}");
                throw new InvalidOperationException($"Failed to configure AI services: {ex.Message}", ex);
            }
        }

        private static void RegisterAzureOpenAIServices(IServiceCollection services, AIConfiguration aiConfig)
        {
            // Register Azure OpenAI client
            services.AddSingleton<OpenAIClient>(sp =>
            {
                return new OpenAIClient(
                    new Uri(aiConfig.Endpoint),
                    new AzureKeyCredential(aiConfig.ApiKey));
            });

            // Register Semantic Kernel for Azure
            services.AddSingleton<Kernel>(sp =>
            {
                var builder = Kernel.CreateBuilder();
                builder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: aiConfig.DeploymentName,
                    endpoint: aiConfig.Endpoint,
                    apiKey: aiConfig.ApiKey);

                return builder.Build();
            });

            // Register Azure OpenAI service
            //   services.AddScoped<AzureOpenAIService>();
            services.AddScoped<IAIService, AzureOpenAIService>();
            Console.WriteLine("[DEBUG] Registered AzureOpenAIService as IAIService");
            services.AddScoped<IAIService>(sp => sp.GetRequiredService<AzureOpenAIService>());

        }

        private static void RegisterOpenAIServices(IServiceCollection services, AIConfiguration aiConfig, IConfiguration configuration)
        {
            // Register HttpClient for OpenAI with Polly policies
            services.AddHttpClient<OpenAIService>(client =>
            {
                var openAISection = configuration.GetSection("AI:OpenAI");
                var baseUrl = openAISection["BaseUrl"] ?? "https://api.openai.com/v1";

                client.BaseAddress = new Uri(baseUrl);
            //    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {aiConfig.ApiKey}");
                client.Timeout = TimeSpan.FromSeconds(aiConfig.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Register Semantic Kernel for OpenAI
            services.AddSingleton<Kernel>(sp =>
            {
                var builder = Kernel.CreateBuilder();
                builder.Services.AddOpenAIChatCompletion(
                    modelId: aiConfig.DeploymentName,
                    apiKey: aiConfig.ApiKey);

                return builder.Build();
            });

            // Register OpenAI service
            //   services.AddScoped<OpenAIService>();
            services.AddScoped<IAIService, OpenAIService>();
            Console.WriteLine("[DEBUG] Registered OpenAIService as IAIService");
            services.AddScoped<IAIService>(sp => sp.GetRequiredService<OpenAIService>());
            Console.WriteLine("[DEBUG] OpenAI service registered successfully");
        }

        //private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        //{
        //    return HttpPolicyExtensions
        //        .HandleTransientHttpError()
        //        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        //        .WaitAndRetryAsync(
        //            3,
        //            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        //            onRetry: (outcome, timespan, retryCount, context) =>
        //            {
        //                var logger = LoggerFactory.Create(builder => builder.AddConsole())
        //                    .CreateLogger<AIServiceExtensions>();

        //                logger.LogWarning(
        //                    "Retry {RetryCount} after {Delay}ms. Status: {StatusCode}",
        //                    retryCount,
        //                    timespan.TotalMilliseconds,
        //                    outcome.Result?.StatusCode);
        //            });
        //}

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        // Don't create logger here - it's static context
                        // Logging will be handled by the service using this policy
                    });
        }

        //private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        //{
        //    return HttpPolicyExtensions
        //        .HandleTransientHttpError()
        //        .CircuitBreakerAsync(
        //            5,
        //            TimeSpan.FromSeconds(30),
        //            onBreak: (result, duration) =>
        //            {
        //                var logger = LoggerFactory.Create(builder => builder.AddConsole())
        //                    .CreateLogger<AIServiceExtensions>();
        //                logger.LogError(
        //                    "Circuit breaker opened for {Duration} seconds. Status: {StatusCode}",
        //                    duration.TotalSeconds,
        //                    result.Result?.StatusCode);
        //            },
        //            onReset: () =>
        //            {
        //                var logger = LoggerFactory.Create(builder => builder.AddConsole())
        //                    .CreateLogger<AIServiceExtensions>();
        //                logger.LogInformation("Circuit breaker reset");
        //            });
        //}

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {

            return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
            //return HttpPolicyExtensions
            //    .HandleTransientHttpError()
            //    .CircuitBreakerAsync(
            //        5,
            //        TimeSpan.FromSeconds(30));
            // Remove onBreak and onReset callbacks that create loggers
        }

        private static void ConfigureCaching(IServiceCollection services, IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            Console.WriteLine($"[CACHE] Attempting to connect to Redis at: {redisConnection}");

            
            Console.WriteLine("=====================================");
            Console.WriteLine($"[CACHE CONFIG] Redis connection string: {redisConnection}");
            Console.WriteLine("=====================================");

            try
            {
                Console.WriteLine("[CACHE CONFIG] Attempting to connect to Redis...");

                // Test Redis connection
                var redis = ConnectionMultiplexer.Connect(redisConnection + ",abortConnect=false,connectTimeout=1000");
                redis.Close();

                Console.WriteLine("[CACHE CONFIG] ✅ SUCCESS! Redis connection successful!");
                Console.WriteLine("[CACHE CONFIG] Using RedisCacheService");

                // If successful, use Redis
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "TechScriptAidAI";
                });

                // Use the dedicated Redis cache service
                services.AddScoped<IAICacheService, RedisCacheService>();
            }
            catch (Exception ex)
            {

                Console.WriteLine($"[CACHE] ❌ Redis connection failed: {ex.Message}");
                Console.WriteLine("[CACHE] ⚠️ Falling back to in-memory cache.");

                // Fallback to memory cache if Redis is not available
                services.AddMemoryCache();

                // Use the dedicated memory cache service
                services.AddScoped<IAICacheService, MemoryCacheService>();
            }
        }

        private static void ConfigureRateLimiting(IServiceCollection services, AIConfiguration aiConfig)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy("ai-policy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = aiConfig.RateLimits.RequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsync(
                        "Rate limit exceeded. Please try again later.", token);
                };
            });
        }

        private static void ConfigureHealthChecks(IServiceCollection services, IConfiguration configuration)
        {
            var healthChecksBuilder = services.AddHealthChecks()
                .AddCheck<AIHealthCheck>("ai_service")
                .AddTypeActivatedCheck<AIProviderHealthCheck>(
                    "ai_provider",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "ai", "live" });

            // Only add Redis health check if connection string exists
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                healthChecksBuilder.AddRedis(
                    redisConnection,
                    name: "redis",
                    timeout: TimeSpan.FromSeconds(3));
            }
        }
    }
}