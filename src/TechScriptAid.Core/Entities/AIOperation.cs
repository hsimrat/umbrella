using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TechScriptAid.Core.Entities
{
    /// <summary>
    /// Represents an AI operation in the system
    /// </summary>
    public class AIOperation : BaseEntity
    {
        public string OperationType { get; set; } = string.Empty;
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string? UserId { get; set; }
        public string Model { get; set; } = string.Empty;
        public string RequestContent { get; set; } = string.Empty;
        public string ResponseContent { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
        public decimal Cost { get; set; }
        public int ResponseTimeMs { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }

        // Navigation properties
        //public virtual User? User { get; set; }
        public virtual ICollection<AIOperationDocument> Documents { get; set; } = new List<AIOperationDocument>();
    }
}
