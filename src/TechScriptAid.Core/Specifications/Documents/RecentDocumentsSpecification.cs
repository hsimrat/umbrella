using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class RecentDocumentsSpecification : BaseSpecification<Document>
    {
        public RecentDocumentsSpecification(int count)
            : base()
        {
            ApplyOrderByDescending(d => d.CreatedAt);
            ApplyPaging(0, count);
        }
    }
}
