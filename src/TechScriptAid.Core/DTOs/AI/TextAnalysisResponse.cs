using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class TextAnalysisResponse : BaseAIResponse
    {
        public SentimentResult? Sentiment { get; set; }
        public string[] KeyPhrases { get; set; } = Array.Empty<string>();
        public Entity[] Entities { get; set; } = Array.Empty<Entity>();
        public string? DetectedLanguage { get; set; }
        public PIIResult[] PIIEntities { get; set; } = Array.Empty<PIIResult>();
        public string Analysis { get; set; } = string.Empty;
    }

    public class SentimentResult
    {
        public string Sentiment { get; set; } = string.Empty;
        public double Positive { get; set; }
        public double Neutral { get; set; }
        public double Negative { get; set; }
    }

    public class Entity
    {
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
    }

    public class PIIResult
    {
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
    }
}
