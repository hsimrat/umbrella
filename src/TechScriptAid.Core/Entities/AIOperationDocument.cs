using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    /// <summary>
    /// Junction table for AI operations and documents
    /// </summary>
    public class AIOperationDocument : BaseEntity
    {
        public Guid AIOperationId { get; set; }  // Changed from int to Guid
        public Guid DocumentId { get; set; }      // Changed from int to Guid
        public string RelationType { get; set; } = string.Empty; // Input, Output, Reference

        // Navigation properties
        public virtual AIOperation AIOperation { get; set; } = null!;
        public virtual Document Document { get; set; } = null!;
    }
}