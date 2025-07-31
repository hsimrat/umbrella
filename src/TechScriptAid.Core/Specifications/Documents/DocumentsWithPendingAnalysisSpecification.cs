using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentsWithPendingAnalysisSpecification : BaseSpecification<Document>
    {
        public DocumentsWithPendingAnalysisSpecification()
            : base(d => d.Analyses.Any(a => a.Status == AnalysisStatus.Pending))
        {
            AddInclude(d => d.Analyses);
            ApplyOrderBy(d => d.CreatedAt);
        }
    }
}
