using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class SemanticSearchRequest : BaseAIRequest
    {
        [Required]
        public string Query { get; set; } = string.Empty;

        [Range(1, 100)]
        public int TopK { get; set; } = 10;

        public string[]? DocumentIds { get; set; }

        public Dictionary<string, object>? Filters { get; set; }

        public double MinimumScore { get; set; } = 0.7;
    }
}
