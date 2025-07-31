using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    public class Document : BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        //public string Category { get; set; } // Add this if needed
        //public DocumentStatus Status { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentHash { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        // Navigation property
        public ICollection<DocumentAnalysis> Analyses { get; set; } = new List<DocumentAnalysis>();
    }

    public enum DocumentStatus
    {
        Draft,
        Published,
        Archived,
        Processing,
        Analyzed
    }
}
