
using System.Linq.Expressions;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Specifications;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentsWithAnalysesSpecification : BaseSpecification<Document>
    {
        public DocumentsWithAnalysesSpecification() : base()
        {
            AddInclude(d => d.Analyses);
            ApplyOrderByDescending(d => d.CreatedAt);
        }
    }
}
