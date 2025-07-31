using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Interfaces.AI
{
    /// <summary>
    /// Logs AI operations for auditing and monitoring
    /// </summary>
    public interface IAIOperationLogger
    {
        /// <summary>
        /// Logs an AI operation
        /// </summary>
        Task LogOperationAsync(AIOperation operation);

        /// <summary>
        /// Gets AI operation history
        /// </summary>
        Task<IEnumerable<AIOperation>> GetOperationHistoryAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Gets AI usage statistics
        /// </summary>
        Task<AIUsageStatistics> GetUsageStatisticsAsync(
            DateTime startDate,
            DateTime endDate);
    }
}
