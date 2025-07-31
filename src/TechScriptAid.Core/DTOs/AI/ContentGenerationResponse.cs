using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class ContentGenerationResponse : BaseAIResponse
    {
        public string GeneratedContent { get; set; } = string.Empty;
        public string[] Suggestions { get; set; } = Array.Empty<string>();
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
