using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    /// <summary>
    /// Stores document embeddings for semantic search
    /// </summary>
    public class DocumentEmbedding : BaseEntity
    {
        public Guid DocumentId { get; set; }
        public string ChunkId { get; set; } = Guid.NewGuid().ToString();
        public string ChunkText { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string Model { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }

        // Navigation property
        public virtual Document Document { get; set; } = null!;
    }

}
