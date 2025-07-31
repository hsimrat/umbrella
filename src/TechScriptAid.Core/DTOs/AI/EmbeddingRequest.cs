using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class EmbeddingRequest : BaseAIRequest
    {
        [Required]
        public string[] Texts { get; set; } = Array.Empty<string>();

        public string Model { get; set; } = "text-embedding-ada-002";
    }
}
