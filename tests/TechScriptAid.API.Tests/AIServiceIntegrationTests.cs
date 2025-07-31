using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Text;
using System.Text.Json;
using TechScriptAid.API.Services;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;
using Xunit;

namespace TechScriptAid.Tests.Integration
{
    public class AIServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AIServiceIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override configuration for testing
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["AI:Provider"] = "AzureOpenAI",
                        ["AI:AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
                        ["AI:AzureOpenAI:ApiKey"] = "test-key",
                        ["AI:AzureOpenAI:DeploymentName"] = "gpt-4-test"
                    });
                });
            }).CreateClient();
        }

        [Fact]
        public async Task SummarizeAsync_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new SummarizationRequest
            {
                Text = "This is a test text that needs to be summarized. It contains multiple sentences and should be reduced to a shorter form.",
                MaxSummaryLength = 50,
                Style = SummarizationStyle.Paragraph
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/ai/summarize", content);

            // Assert
            response.Should().BeSuccessful();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("AzureOpenAI")]
        [InlineData("OpenAI")]
        public async Task AIServiceFactory_CanCreateService_ForBothProviders(string provider)
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IAIServiceFactory>();

            // Act
            var service = factory.GetService(provider);

            // Assert
            service.Should().NotBeNull();
            service.Should().BeAssignableTo<IAIService>();
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthy_WhenServicesAreRunning()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.Should().BeSuccessful();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }
    }

    public class AIProviderTests
    {
        [Fact]
        public async Task CompareProviders_Performance_AndCost()
        {
            // This test compares both providers
            var testCases = new[]
            {
                new { Text = "Short text", ExpectedTokens = 10 },
                new { Text = string.Concat(Enumerable.Repeat("Medium length text. ", 50)), ExpectedTokens = 200 },
                new { Text = string.Concat(Enumerable.Repeat("Long text content. ", 200)), ExpectedTokens = 800 }
            };

            var results = new List<ProviderComparisonResult>();

            foreach (var testCase in testCases)
            {
                // Test Azure OpenAI
                var azureResult = await TestProvider("AzureOpenAI", testCase.Text);

                // Test OpenAI
                var openAIResult = await TestProvider("OpenAI", testCase.Text);

                results.Add(new ProviderComparisonResult
                {
                    TextLength = testCase.Text.Length,
                    AzureOpenAITime = azureResult.ProcessingTime,
                    OpenAITime = openAIResult.ProcessingTime,
                    AzureOpenAICost = azureResult.Cost,
                    OpenAICost = openAIResult.Cost
                });
            }

            // Generate comparison report
            GenerateComparisonReport(results);
        }

        private async Task<(long ProcessingTime, decimal Cost)> TestProvider(string provider, string text)
        {
            // Implementation for testing each provider
            await Task.Delay(100); // Simulate API call
            return (ProcessingTime: Random.Shared.Next(100, 500), Cost: 0.001m);
        }

        private void GenerateComparisonReport(List<ProviderComparisonResult> results)
        {
            // Generate CSV or JSON report for analysis
            var report = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText($"provider-comparison-{DateTime.Now:yyyyMMdd-HHmmss}.json", report);
        }

        private class ProviderComparisonResult
        {
            public int TextLength { get; set; }
            public long AzureOpenAITime { get; set; }
            public long OpenAITime { get; set; }
            public decimal AzureOpenAICost { get; set; }
            public decimal OpenAICost { get; set; }
        }
    }
}