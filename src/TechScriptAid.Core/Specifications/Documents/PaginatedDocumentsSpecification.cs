using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class PaginatedDocumentsSpecification : BaseSpecification<Document>
    {
        public PaginatedDocumentsSpecification(int pageNumber, int pageSize)
            : base()
        {
            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
            ApplyOrderByDescending(d => d.CreatedAt);
        }

        public PaginatedDocumentsSpecification(int pageNumber, int pageSize,
            Expression<Func<Document, bool>> criteria) : base(criteria)
        {
            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
            ApplyOrderByDescending(d => d.CreatedAt);
        }
    }
}
