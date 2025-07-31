using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class TextAnalysisRequest : BaseAIRequest
    {
        [Required]
        [MaxLength(10000)]
        public string Text { get; set; } = string.Empty;

        public AnalysisType[] AnalysisTypes { get; set; } = new[] {
                AnalysisType.Sentiment,
                AnalysisType.Keywords, // Fixed: Changed 'KeyPhrases' to 'Keywords' to match the enum definition
                AnalysisType.Entities
            };

        public string Language { get; set; } = "en";
    }

    //public enum AnalysisType
    //{
    //    Sentiment,
    //    KeyPhrases,
    //    Entities,
    //    LanguageDetection,
    //    PII,
    //    Summary
    //}
}
