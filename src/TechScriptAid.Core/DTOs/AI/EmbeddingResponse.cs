using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class EmbeddingResponse : BaseAIResponse
    {
        public EmbeddingData[] Embeddings { get; set; } = Array.Empty<EmbeddingData>();

    }

    public class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string Object { get; set; } = "embedding";
        public double[] Vector { get; set; } = Array.Empty<double>();
    }
}
