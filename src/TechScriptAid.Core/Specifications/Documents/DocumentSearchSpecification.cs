using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentSearchSpecification : BaseSpecification<Document>
    {
        public DocumentSearchSpecification(string searchTerm)
            : base(d => string.IsNullOrEmpty(searchTerm) ||
                       d.Title.Contains(searchTerm) ||
                       d.Description.Contains(searchTerm) ||
                       d.Content.Contains(searchTerm))
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                AddInclude(d => d.Analyses);
            }
            ApplyOrderByDescending(d => d.CreatedAt);
        }
    }
}
