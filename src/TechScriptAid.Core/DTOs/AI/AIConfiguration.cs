using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class AIConfiguration
    {
        public string Provider { get; set; } = string.Empty; // Fix: Initialize with a default value
        public string Endpoint { get; set; } = string.Empty; // Fix: Initialize with a default value
        public string ApiKey { get; set; } = string.Empty; // Fix: Initialize with a default value
        public string DeploymentName { get; set; } = string.Empty; // Fix: Initialize with a default value
        public string ApiVersion { get; set; } = string.Empty; // Fix: Initialize with a default value
        public int MaxRetries { get; set; }
        public int TimeoutSeconds { get; set; }
        //  public RateLimitConfiguration RateLimits { get; set; } = new RateLimitConfiguration(); // Fix: Initialize with a default instance
        public RateLimitConfiguration RateLimits { get; set; } = new();
    }

}

public class RateLimitConfiguration
{
    //public int ConcurrentRequests { get; set; } = 10;
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int TokensPerMinute { get; set; } = 0; // Fix: Initialize with a default value
    public int ConcurrentRequests { get; set; }
}
    //public class RateLimitConfiguration
    //{
    //    public int ConcurrentRequests { get; set; } = 10;
    //    public int RequestsPerMinute { get; set; } = 60;

    //    public int RequestsPerHour { get; set; } = 1000;
    //}

    // Requests
    //public class SummarizationRequest
    //{
    //    public string Text { get; set; } = string.Empty;
    //    public SummarizationStyle Style { get; set; } = SummarizationStyle.Balanced;
    //    public int MaxSummaryLength { get; set; } = 150;
    //    public string? UserId { get; set; }
    //    public string? CorrelationId { get; set; }
    //}

    //public class ContentGenerationRequest
    //{
    //    public string Prompt { get; set; } = string.Empty;
    //    public ContentType ContentType { get; set; } = ContentType.General;
    //    public string? SystemPrompt { get; set; }
    //    public Dictionary<string, string>? Parameters { get; set; }
    //    public double Temperature { get; set; } = 0.7;
    //    public int MaxTokens { get; set; } = 1000;
    //    public string? UserId { get; set; }
    //    public string? CorrelationId { get; set; }
    //}

    //public class TextAnalysisRequest
    //{
    //    public string Text { get; set; } = string.Empty;
    //    public List<AnalysisType> AnalysisTypes { get; set; } = new();
    //    public string? UserId { get; set; }
    //    public string? CorrelationId { get; set; }
    //}

   

    //public class SemanticSearchRequest
    //{
    //    public string Query { get; set; } = string.Empty;
    //    public int TopK { get; set; } = 10;
    //    public double MinimumScore { get; set; } = 0.7;
    //    public string[]? DocumentIds { get; set; }
    //    public Dictionary<string, object>? Filters { get; set; }
    //    public string? UserId { get; set; }
    //    public string? CorrelationId { get; set; }
    //}

    //// Responses
    //public class SummarizationResponse
    //{
    //    public string Summary { get; set; } = string.Empty;
    //    public string[] KeyPoints { get; set; } = Array.Empty<string>();
    //    public double ConfidenceScore { get; set; }
    //    public string? Model { get; set; }
    //    public TokenUsage? Usage { get; set; }
    //    public long ProcessingTimeMs { get; set; }
    //    public string? CorrelationId { get; set; }
    //}

   

    //public class EmbeddingResponse
    //{
    //    public EmbeddingData[] Embeddings { get; set; } = Array.Empty<EmbeddingData>();
    //    public string? Model { get; set; }
    //    public TokenUsage? Usage { get; set; }
    //    public long ProcessingTimeMs { get; set; }
    //    public string? CorrelationId { get; set; }
    //}


    // Supporting types
    //public class TokenUsage
    //{
    //    public int PromptTokens { get; set; }
    //    public int CompletionTokens { get; set; }
    //    public int TotalTokens => PromptTokens + CompletionTokens;
    //    public decimal EstimatedCost { get; set; }
    //}

    //public class EmbeddingData
    //{
    //    public int Index { get; set; }
    //    public float[] Embedding { get; set; } = Array.Empty<float>();
    //}

   
    // Enums
    //public enum SummarizationStyle
    //{
    //    Balanced,
    //    Bullet,
    //    Paragraph,
    //    Executive,
    //    Technical
    //}

    //public enum ContentType
    //{
    //    General,
    //    Technical,
    //    Creative,
    //    Business,
    //    Academic,
    //    Marketing,
    //    Code
    //}

    //public enum AnalysisType
    //{
    //    Sentiment,
    //    Keywords,
    //    Entities,
    //    Topics,
    //    Language,
    //    Complexity
    //}

