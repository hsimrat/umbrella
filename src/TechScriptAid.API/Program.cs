// Third-party namespaces
using Asp.Versioning;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;   
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System;
using System.IO;
using System.Text;
using System.Threading.RateLimiting;
using TechScriptAid.AI.Services;
// Project-specific namespaces
using TechScriptAid.API.Extensions;
using TechScriptAid.API.Middleware;
using TechScriptAid.API.Monitoring;
using TechScriptAid.API.Security;
using TechScriptAid.API.Services;
using TechScriptAid.Core.Interfaces;
using TechScriptAid.Core.Interfaces.AI;
using TechScriptAid.Infrastructure.Data;
using TechScriptAid.Infrastructure.Data.Seeding;
using TechScriptAid.Infrastructure.Repositories;

// --- Main Application Setup ---

// Create the WebApplicationBuilder
var builder = WebApplication.CreateBuilder(args);

// --- Serilog Configuration ---
// Configure Serilog directly on the host for robust logging from the start.
// This is the recommended approach over the static Log.Logger.
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration) // Read configuration from appsettings.json
    .ReadFrom.Services(services) // Allow services to be injected into sinks
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day, // Creates a new log file daily
        retainedFileCountLimit: 30, // Keeps logs for 30 days
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
);

try
{
    Log.Information("Configuring web application...");

    // --- Service Registration (Dependency Injection) ---

    // Add MVC Controllers
    builder.Services.AddControllers();

    // Add AutoMapper for object-to-object mapping
    builder.Services.AddAutoMapper(typeof(Program));

    // Add API Explorer for Swagger
    builder.Services.AddEndpointsApiExplorer();
        
    // Configure Swagger/OpenAPI for API documentation
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TechScriptAid Enterprise AI API",
            Version = "v1",
            Description = "Enterprise-grade AI document processing API with Azure OpenAI integration.",
            Contact = new OpenApiContact { Name = "Harsimrat Singh", Email = "techscriptaid@gmail.com" }
        });

        c.CustomSchemaIds(type =>
        {
            // Include namespace in schema ID to avoid conflicts
            var schemaId = type.Name;
            if (type.IsNested)
            {
                schemaId = type.DeclaringType?.Name + schemaId;
            }

            // Handle specific conflicts
            if (type.FullName != null)
            {
                if (type.FullName.Contains("TechScriptAid.Core.Entities") && type.Name == "AnalysisType")
                {
                    return "EntityAnalysisType";
                }
                else if (type.FullName.Contains("TechScriptAid.Core.DTOs.AI") && type.Name == "AnalysisType")
                {
                    return "AIAnalysisType";
                }
            }

            return schemaId;
        });



        // Enable JWT Bearer authentication in Swagger UI
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });


        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments for controller actions in Swagger
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Configure Entity Framework Core DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("TechScriptAid.Infrastructure");
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });

        // Log sensitive data only in the Development environment
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // Add Memory Cache
    builder.Services.AddMemoryCache();

    // Configure Repositories and Unit of Work
    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Configure Application Services
    builder.Services.AddScoped<IDocumentService, DocumentService>();

    // Add custom Data Seeder
    builder.Services.AddDataSeeder();

    // Add Health Checks for the database and other services
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database_health_check", tags: new[] { "database" });

    // Configure CORS (Cross-Origin Resource Sharing)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000", "https://localhost:3000", // Common React dev ports
                    "http://localhost:5173", "https://localhost:5173", // Common Vite/React dev ports
                     "http://localhost:5000", "https://localhost:7001"  // Added these
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Add API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        // FIX: Fully qualify ApiVersion to resolve ambiguity between 'Microsoft.AspNetCore.Mvc.ApiVersion' and 'Asp.Versioning.ApiVersion'
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Add Response Caching and Compression
    builder.Services.AddResponseCaching();
    builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });

    // Register AI services using the custom extension method
    builder.Services.AddAIServices(builder.Configuration);


    // Add this for debugging
Console.WriteLine("=====================================");
Console.WriteLine("[STARTUP] Checking cache service registration...");


    var serviceDescriptor = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IAICacheService));
    if (serviceDescriptor != null)
    {
        Console.WriteLine($"[STARTUP] Cache service registered: {serviceDescriptor.ImplementationType?.Name}");
        Console.WriteLine($"[STARTUP] Service lifetime: {serviceDescriptor.Lifetime}");
    }
    else
    {
        Console.WriteLine("[STARTUP] WARNING: No cache service registered!");
    }
    Console.WriteLine("=====================================");

    // Temporary: Force Redis registration
    var testRedis = ConnectionMultiplexer.Connect("localhost:6379");
    Console.WriteLine($"[PROGRAM.CS] Redis test connection: {testRedis.IsConnected}");
    testRedis.Close();

    // Ensure IAIService is registered (fallback)
    builder.Services.TryAddScoped<IAIService, OpenAIService>();

    // Temporary fix - ensure IAIService is registered
    if (!builder.Services.Any(s => s.ServiceType == typeof(IAIService)))
    {
        var provider = builder.Configuration["AI:Provider"] ?? "OpenAI";
        if (provider == "AzureOpenAI")
        {
            builder.Services.AddScoped<IAIService, AzureOpenAIService>();
        }
        else
        {
            builder.Services.AddScoped<IAIService, OpenAIService>();
        }
        Console.WriteLine($"[DEBUG] Manually registered IAIService for {provider}");
    }
    // Register Redis cache service
    builder.Services.AddSingleton<IAICacheService, RedisCacheService>();


   // builder.Services.AddHttpClient<OpenAIService>();
    //builder.Services.AddScoped<IAIService, OpenAIService>();


    //    builder.Services.AddAuthentication(options =>
    //    {
    //        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    //    })
    //.AddJwtBearer(options =>
    //{
    //    options.TokenValidationParameters = new TokenValidationParameters
    //    {
    //        ValidateIssuer = true,
    //        ValidateAudience = true,
    //        ValidateLifetime = true,
    //        ValidateIssuerSigningKey = true,

    //        ValidIssuer = "your-app-name",             // Replace with your issuer
    //        ValidAudience = "your-app-client",         // Replace with your audience
    //        IssuerSigningKey = new SymmetricSecurityKey(
    //            Encoding.UTF8.GetBytes("super-secret-key-dont-share")) // Store in config
    //    };
    //});

    //  builder.Services.AddAuthorization();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

    builder.Services.AddAuthorization();

   // builder.Configuration.AddSecureConfiguration(builder.Environment);

    // Add these services
    builder.Services.AddSecureAIConfiguration();
    // builder.Services.AddSingleton<IOptimizationService, OptimizationService>();
    builder.Services.AddScoped<IOptimizationService, OptimizationService>();
    //builder.Services.AddSingleton<IApiKeyRotationService, ApiKeyRotationService>();

    builder.Services.AddScoped<IApiKeyRotationService, ApiKeyRotationService>();


    builder.Services.AddRateLimiter(options =>
    {
        // Global rate limit
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));


        // This is my Per-user rate limit as i shown 
        // Per-user rate limit
        options.AddPolicy("PerUser", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));


        // AI endpoint specific
        //options.AddPolicy("ai-policy", httpContext =>
        //RateLimitPartition.GetSlidingWindowLimiter(
        //    partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
        //    factory: partition => new SlidingWindowRateLimiterOptions
        //    {
        //        AutoReplenishment = true,
        //        PermitLimit = 20,
        //        Window = TimeSpan.FromMinutes(1),
        //        SegmentsPerWindow = 4
        //    }));

        //options.AddPolicy("ai-policy", httpContext =>
        //RateLimitPartition.GetFixedWindowLimiter(
        //    partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
        //    factory: partition => new FixedWindowRateLimiterOptions
        //    {
        //        AutoReplenishment = true,
        //        PermitLimit = 60,  // 60 requests per minute for AI endpoints
        //        Window = TimeSpan.FromMinutes(1)
        //    }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsync(
                "Rate limit exceeded. Please try again later.", token);
        };
});

    // --- Build the Application ---
    var app = builder.Build();

    // Test cache service immediately after building
    using (var scope = app.Services.CreateScope())
    {
        var cacheService = scope.ServiceProvider.GetRequiredService<IAICacheService>();
        Console.WriteLine($"[STARTUP CHECK] Cache implementation: {cacheService.GetType().Name}");

        // Try to connect to Redis directly
        try
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false,connectTimeout=2000");
            Console.WriteLine($"[STARTUP CHECK] Direct Redis connection: SUCCESS");
            redis.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[STARTUP CHECK] Direct Redis connection: FAILED - {ex.Message}");
        }
    }

    var serviceDescriptors = builder.Services
    .Where(s => s.ServiceType.Namespace?.Contains("TechScriptAid") == true)
    .Select(s => $"{s.ServiceType.Name} -> {s.ImplementationType?.Name} ({s.Lifetime})")
    .ToList();

    foreach (var descriptor in serviceDescriptors)
    {
        Console.WriteLine(descriptor);
    }

    // --- Middleware Pipeline Configuration ---

    // Seed the database on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            //var context = services.GetRequiredService<ApplicationDbContext>();
            //if (app.Environment.IsDevelopment())
            //{
            //    // In dev, ensure the DB is created (useful for non-migration scenarios)
            //    await context.Database.EnsureCreatedAsync();
            //}
            // You can apply migrations programmatically here if needed for production
            // await context.Database.MigrateAsync();

          //  await services.SeedDatabaseAsync(); // Call the data seeder
            Log.Information("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred during database seeding.");
        }
    }

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechScriptAid API V1");
            c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        });
    }
    else
    {
        // Use custom error handling middleware in Production
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseHsts();
    }

    // Add Serilog request logging for detailed HTTP request logs
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseResponseCompression();
    app.UseResponseCaching();
    app.UseCors("AllowSpecificOrigins");

    // Add custom error handling middleware (can be used in dev too if preferred over UseDeveloperExceptionPage)
    // app.UseMiddleware<ErrorHandlingMiddleware>();


    

    app.UseAuthentication(); // Uncomment if you add JWT authentication
    app.UseAuthorization();

    app.UseRateLimiter();

    // --- Endpoint Mapping ---
    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        Predicate = _ => true // Include all registered health checks
    });

    // Redirect root to Swagger UI
    app.MapGet("/", () => Results.Redirect("/index.html"))
       .ExcludeFromDescription();

    // Add middleware (after app.UseRateLimiter())
    app.UseMiddleware<MetricsMiddleware>();

    // Add dashboard endpoint
    app.UseHealthCheckDashboard();

    // Add optimization background service (optional)
    //    builder.Services.AddHostedService<OptimizationBackgroundService>();

    // --- Run the Application ---
    Log.Information("Starting TechScriptAid Enterprise AI API");
    app.Run();

}
catch (Exception ex)
{
    // Log any fatal exceptions that occur during startup
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    // Ensure all logs are flushed on application shutdown
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }
