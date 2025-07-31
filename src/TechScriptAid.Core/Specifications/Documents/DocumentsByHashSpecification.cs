using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentsByHashSpecification : BaseSpecification<Document>
    {
        public DocumentsByHashSpecification(string contentHash)
            : base(d => d.ContentHash == contentHash)
        {
        }
    }
}
