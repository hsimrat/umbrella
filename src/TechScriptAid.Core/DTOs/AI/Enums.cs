using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public enum SummarizationStyle
    {
        Balanced,
        Bullet,
        Paragraph,
        Executive,
        Technical
    }

    public enum ContentType
    {
        General,
        Technical,
        Creative,
        Business,
        Academic,
        Marketing,
        Code
    }

    public enum AnalysisType
    {
        Sentiment,
        Keywords,
        Entities,
        Topics,
        Language,
        Complexity
    }
}
