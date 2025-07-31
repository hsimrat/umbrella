using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Specifications;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentsByDateRangeSpecification : BaseSpecification<Document>
    {
        public DocumentsByDateRangeSpecification(DateTime startDate, DateTime endDate)
            : base(d => d.CreatedAt >= startDate && d.CreatedAt <= endDate)
        {
            ApplyOrderByDescending(d => d.CreatedAt);
        }
    }
}
