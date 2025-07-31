using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class SummarizationRequest : BaseAIRequest
    {
        [Required]
        [MinLength(50, ErrorMessage = "Text must be at least 50 characters for meaningful summarization")]
        [MaxLength(50000, ErrorMessage = "Text exceeds maximum length of 50,000 characters")]
        public string Text { get; set; } = string.Empty;

        [Range(50, 1000)]
        public int MaxSummaryLength { get; set; } = 200;

        public SummarizationStyle Style { get; set; } = SummarizationStyle.Balanced;

        public string? Language { get; set; } = "en";
    }

    //public enum SummarizationStyle
    //{
    //    Bullet,
    //    Paragraph,
    //    Executive,
    //    Technical,
    //    Balanced
    //}
}
