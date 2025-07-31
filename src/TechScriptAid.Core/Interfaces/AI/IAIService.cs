using System.Threading;
using System.Threading.Tasks;
using TechScriptAid.Core.DTOs.AI;


namespace TechScriptAid.Core.Interfaces.AI
{
    /// <summary>
    /// Defines the contract for AI services in the application
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Generates a summary of the provided text
        /// </summary>
        /// <param name="request">The summarization request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The summarization response</returns>
        Task<SummarizationResponse> SummarizeAsync(
            SummarizationRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates content based on the provided prompt
        /// </summary>
        /// <param name="request">The content generation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The generated content response</returns>
        Task<ContentGenerationResponse> GenerateContentAsync(
            ContentGenerationRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes text for sentiment, key phrases, and entities
        /// </summary>
        /// <param name="request">The text analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The analysis response</returns>
        Task<TextAnalysisResponse> AnalyzeTextAsync(
            TextAnalysisRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates embeddings for the provided text
        /// </summary>
        /// <param name="request">The embedding request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The embedding response</returns>
        Task<EmbeddingResponse> GenerateEmbeddingAsync(
            EmbeddingRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs semantic search on documents
        /// </summary>
        /// <param name="request">The semantic search request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The search results</returns>
        Task<SemanticSearchResponse> SemanticSearchAsync(
            SemanticSearchRequest request,
            CancellationToken cancellationToken = default);
    }

    //public interface IAIOperationLogger
    //{
    //    Task LogOperationAsync(AIOperation operation);
    //}

    //public interface IAICacheService
    //{
    //    string GenerateCacheKey(string operation, object request);
    //    Task<T?> GetAsync<T>(string key) where T : class;
    //    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
    //}

    //public interface ITokenCalculator
    //{
    //    int EstimateTokens(string text);
    //    bool IsWithinTokenLimit(string text, int maxTokens);
    //    decimal CalculateCost(int promptTokens, int completionTokens, string model);
    //}
}
