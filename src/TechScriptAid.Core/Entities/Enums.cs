using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Entities
{
    public enum DocumentType
    {
        TechnicalDocument,
        Report,
        Presentation,
        Spreadsheet,
        Email,
        Memo,
        Contract,
        Other
    }

    public enum AnalysisType
    {
        Summary,
        KeywordExtraction,
        SentimentAnalysis,
        Translation,
        Classification,
        QuestionAnswering,
        ContentModeration,
        EntityRecognition,
        Custom
    }

    public enum AnalysisStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Queued,
        Retrying
    }
}
