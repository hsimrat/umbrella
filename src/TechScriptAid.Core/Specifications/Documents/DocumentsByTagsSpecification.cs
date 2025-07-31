using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentsByTagsSpecification : BaseSpecification<Document>
    {
        public DocumentsByTagsSpecification(params string[] tags)
            : base(d => tags.Any(tag => d.Tags.Contains(tag)))
        {
            ApplyOrderByDescending(d => d.CreatedAt);
        }
    }
}
