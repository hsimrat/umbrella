using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class ContentGenerationRequest : BaseAIRequest
    {
        [Required]
        [MaxLength(2000)]
        public string Prompt { get; set; } = string.Empty;

        [Range(50, 4000)]
        public int MaxTokens { get; set; } = 500;

        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 0.7;

        public ContentType ContentType { get; set; } = ContentType.General;

        public string? SystemPrompt { get; set; }

        public Dictionary<string, string>? Parameters { get; set; }
    }

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
}
