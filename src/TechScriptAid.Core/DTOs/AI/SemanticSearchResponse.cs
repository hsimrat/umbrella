using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class SemanticSearchResponse : BaseAIResponse
    {
        public SearchResult[] Results { get; set; } = Array.Empty<SearchResult>();
        public int TotalResults { get; set; }
    }

    public class SearchResult
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
