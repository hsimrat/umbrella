using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Specifications.Documents
{
    public class DocumentsWithAnalysisTypeSpecification : BaseSpecification<Document>
    {
        public DocumentsWithAnalysisTypeSpecification(AnalysisType analysisType)
            : base(d => d.Analyses.Any(a => a.AnalysisType == analysisType))
        {
            AddInclude(d => d.Analyses.Where(a => a.AnalysisType == analysisType));
            ApplyOrderByDescending(d => d.CreatedAt);
        }
    }
}
