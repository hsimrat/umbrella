using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TechScriptAid.Core.DTOs.AI
{
    /// <summary>
    /// Base class for all AI requests
    /// </summary>
    public abstract class BaseAIRequest
    {
        /// <summary>
        /// Optional user ID for tracking
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Optional correlation ID for request tracking
        /// </summary>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Optional metadata for the request
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

}
