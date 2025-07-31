using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    /// <summary>
    /// Caches AI responses for performance optimization
    /// </summary>
    public class AICache : BaseEntity
    {
        public string CacheKey { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string RequestHash { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int HitCount { get; set; }
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
