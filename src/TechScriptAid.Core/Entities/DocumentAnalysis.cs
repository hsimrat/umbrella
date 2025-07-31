using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;

namespace TechScriptAid.Core.Entities
{
    public class DocumentAnalysis : BaseEntity
    {
        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        public AnalysisType AnalysisType { get; set; }
        public AnalysisStatus Status { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? DurationInSeconds { get; set; }

        // AI-related properties
        public string ModelUsed { get; set; }
        public string ModelVersion { get; set; }
        public string Prompt { get; set; }
        public int? TokensUsed { get; set; }
        public decimal? Cost { get; set; }

        // Results
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public string Summary { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public string Sentiment { get; set; }
        public decimal? SentimentScore { get; set; }

        // Error handling
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
    }


}
