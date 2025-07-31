using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    /// <summary>
    /// Version history for AI prompt templates
    /// </summary>
    public class AIPromptTemplateVersion : BaseEntity
    {
        public Guid PromptTemplateId { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string ChangeDescription { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual AIPromptTemplate PromptTemplate { get; set; } = null!;
    }
}
