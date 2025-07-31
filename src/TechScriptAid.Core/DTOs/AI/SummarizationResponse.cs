using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class SummarizationResponse : BaseAIResponse
    {
        public string Summary { get; set; } = string.Empty;
        public string[] KeyPoints { get; set; } = Array.Empty<string>();
        public double ConfidenceScore { get; set; }
    }
}
