using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Interfaces.AI
{
    /// <summary>
    /// Calculates token usage for AI operations
    /// </summary>
    public interface ITokenCalculator
    {
        /// <summary>
        /// Estimates tokens for a given text
        /// </summary>
        int EstimateTokens(string text);

        /// <summary>
        /// Calculates cost based on token usage
        /// </summary>
        decimal CalculateCost(int promptTokens, int completionTokens, string model);

        /// <summary>
        /// Validates if text is within token limits
        /// </summary>
        bool IsWithinTokenLimit(string text, int maxTokens);
    }
}
