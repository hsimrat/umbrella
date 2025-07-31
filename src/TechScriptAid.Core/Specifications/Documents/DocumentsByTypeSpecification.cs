using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;
using System.Linq.Expressions;
using TechScriptAid.Core.Specifications;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentsByTypeSpecification : BaseSpecification<Document>
    {
        public DocumentsByTypeSpecification(DocumentType documentType)
            : base(d => d.DocumentType == documentType)
        {
            ApplyOrderByDescending(d => d.CreatedAt);
        }
    }
}
