# TechScriptAid API Test Script for YouTube Demo

try
    {
        // Temporarily add just the basic services to isolate the issue
        services.AddSingleton<ITokenCalculator, TokenCalculator>();
        services.AddScoped<IAIOperationLogger, AIOperationLogger>();
        services.AddScoped<IAIConfigurationService, AIConfigurationService>();
        
        // Add memory cache as default
        services.AddMemoryCache();
        services.AddScoped<IAICacheService, MemoryCacheService>();
        
        // Add a simple version first
        var provider = configuration["AI:Provider"] ?? "OpenAI";
        if (provider == "OpenAI")
        {
            services.AddHttpClient<OpenAIService>();
            services.AddScoped<IAIService, OpenAIService>();
        }
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<AIHealthCheck>("ai_service");
        
        return services;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to configure AI services: {ex.Message}", ex);
    }

Start-Sleep -Seconds 2

# 2. Compare Providers
$compareBody = @{
    text = "Artificial intelligence is revolutionizing how we work and solve problems."
}
Test-Endpoint -Name "Compare AI Providers" -Method "POST" `
    -Endpoint "/api/v1/ai-test/compare-providers" -Body $compareBody

Start-Sleep -Seconds 2

# 3. Demo Caching (run twice to show cache hit)
Write-Host "`n?? Testing Caching (First Call - Cache Miss)" -ForegroundColor Magenta
$cacheTest = Test-Endpoint -Name "All Patterns Demo" -Method "POST" `
    -Endpoint "/api/v1/ai-test/demo-all-patterns" `
    -Body "Testing caching behavior"

Start-Sleep -Seconds 1

Write-Host "`n?? Testing Caching (Second Call - Cache Hit)" -ForegroundColor Magenta
$cacheTest2 = Test-Endpoint -Name "All Patterns Demo" -Method "POST" `
    -Endpoint "/api/v1/ai-test/demo-all-patterns" `
    -Body "Testing caching behavior"

# 4. Test Circuit Breaker
$circuitBody = @{
    numberOfRequests = 10
    simulateFailure = $true
    failuresCount = 6
    delayBetweenRequests = 100
}
Test-Endpoint -Name "Circuit Breaker Test" -Method "POST" `
    -Endpoint "/api/v1/ai-test/test-circuit-breaker" -Body $circuitBody

# 5. Show Metrics
Test-Endpoint -Name "Performance Metrics" -Method "GET" `
    -Endpoint "/api/v1/ai-test/metrics"

Write-Host "`n? Demo Complete!" -ForegroundColor Green
Write-Host "Check out the health dashboard at: https://localhost:7001/health/dashboard" -ForegroundColor Cyan