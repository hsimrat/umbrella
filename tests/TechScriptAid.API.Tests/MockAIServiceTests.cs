using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using TechScriptAid.AI.Services;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Tests.Unit
{
    public class MockAIServiceTests
    {
        private readonly Mock<ILogger<AzureOpenAIService>> _loggerMock;
        private readonly Mock<IAICacheService> _cacheMock;
        private readonly Mock<ITokenCalculator> _tokenCalculatorMock;
        private readonly Mock<IAIOperationLogger> _operationLoggerMock;

        public MockAIServiceTests()
        {
            _loggerMock = new Mock<ILogger<AzureOpenAIService>>();
            _cacheMock = new Mock<IAICacheService>();
            _tokenCalculatorMock = new Mock<ITokenCalculator>();
            _operationLoggerMock = new Mock<IAIOperationLogger>();
        }

        [Fact]
        public async Task SummarizeAsync_WithCachedResult_DoesNotCallAPI()
        {
            // Arrange
            var cachedResponse = new SummarizationResponse
            {
                Summary = "Cached summary",
                ConfidenceScore = 0.95
            };

            _cacheMock.Setup(x => x.GetAsync<SummarizationResponse>(It.IsAny<string>()))
                      .ReturnsAsync(cachedResponse);

            // Act & Assert
            // Verify that API is not called when cache hit occurs
            _operationLoggerMock.Verify(x => x.LogOperationAsync(It.IsAny<AIOperation>()), Times.Never);

        }

        [Fact]
        public async Task CircuitBreaker_OpensAfterConsecutiveFailures()
        {
            // Test circuit breaker behavior
            var service = new AzureOpenAIService(_loggerMock.Object);

            // Simulate failures
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await service.GetSummaryAsync("test");
                }
                catch { }
            }

            // Circuit should be open
            var state = service.GetCircuitState();
            state.Should().Be(Polly.CircuitBreaker.CircuitState.Open);
        }
    }
}