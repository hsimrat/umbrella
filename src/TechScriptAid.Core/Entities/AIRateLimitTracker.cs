using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    /// <summary>
    /// Tracks rate limits for AI operations
    /// </summary>
    public class AIRateLimitTracker : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty; // API endpoint or operation type
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
        public int TokenCount { get; set; }
        public DateTime LastRequestAt { get; set; }
        public bool IsThrottled { get; set; }
        public DateTime? ThrottledUntil { get; set; }
    }
}
