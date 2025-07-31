using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    /// <summary>
    /// Stores reusable AI prompt templates
    /// </summary>
    public class AIPromptTemplate : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string? SystemPrompt { get; set; }
        public double DefaultTemperature { get; set; } = 0.7;
        public int DefaultMaxTokens { get; set; } = 500;
        public string Version { get; set; } = "1.0";
        public DateTime LastUsed { get; set; }
        public int UsageCount { get; set; }

        // Navigation properties
        public virtual ICollection<AIPromptTemplateVersion> Versions { get; set; } = new List<AIPromptTemplateVersion>();
    }
}
