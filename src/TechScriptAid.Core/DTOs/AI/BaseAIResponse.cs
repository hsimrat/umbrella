using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    /// <summary>
    /// Base class for all AI responses
    /// </summary>
    public abstract class BaseAIResponse
    {
        /// <summary>
        /// Unique identifier for the response
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when the response was generated
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Token usage information
        /// </summary>
        public TokenUsage? Usage { get; set; }

        /// <summary>
        /// The model used for generation
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Correlation ID from the request
        /// </summary>
        public string? CorrelationId { get; set; }
    }
}
