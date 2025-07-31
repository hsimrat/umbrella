using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Interfaces;
using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.AI.Services
{
    /// <summary>
    /// Logs and tracks AI operations
    /// </summary>
    public class AIOperationLogger : IAIOperationLogger
    {
        private readonly ILogger<AIOperationLogger> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public AIOperationLogger(ILogger<AIOperationLogger> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task LogOperationAsync(AIOperation operation)
        {
            try
            {
                var repository = _unitOfWork.Repository<AIOperation>();
                await repository.AddAsync(operation);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log AI operation");
            }
        }

        public async Task<IEnumerable<AIOperation>> GetOperationsAsync(string? userId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var repository = _unitOfWork.Repository<AIOperation>();
            var operations = await repository.GetAllAsync();

            if (!string.IsNullOrEmpty(userId))
                operations = operations.Where(o => o.UserId == userId).ToList();

            if (startDate.HasValue)
                operations = operations.Where(o => o.CreatedAt >= startDate.Value).ToList();

            if (endDate.HasValue)
                operations = operations.Where(o => o.CreatedAt <= endDate.Value).ToList();

            return operations;
        }

        public async Task<AIUsageStatistics> GetUsageStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var repository = _unitOfWork.Repository<AIOperation>();
            var operations = await repository.GetAllAsync();

            var filteredOps = operations
                .Where(o => o.Timestamp >= startDate && o.Timestamp <= endDate)
                .ToList();

            var statistics = new AIUsageStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRequests = filteredOps.Count,
                SuccessfulRequests = filteredOps.Count(o => o.IsSuccessful),
                FailedRequests = filteredOps.Count(o => !o.IsSuccessful),
                TotalTokensUsed = filteredOps.Sum(o => o.TotalTokens),
                TotalCost = filteredOps.Sum(o => o.Cost),
                RequestsByOperation = filteredOps
                    .GroupBy(o => o.OperationType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TokensByOperation = filteredOps
                    .GroupBy(o => o.OperationType)
                    .ToDictionary(g => g.Key, g => (long)g.Sum(o => o.TotalTokens)),
                AverageResponseTimeMs = filteredOps.Any()
                    ? filteredOps.Average(o => o.ResponseTimeMs)
                    : 0
            };

            return statistics;
        }

        public async Task<IEnumerable<AIOperation>> GetOperationHistoryAsync(int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null)
        {
            var repository = _unitOfWork.Repository<AIOperation>();
            var operations = await repository.GetAllAsync();

            if (startDate.HasValue)
                operations = operations.Where(o => o.CreatedAt >= startDate.Value).ToList();

            if (endDate.HasValue)
                operations = operations.Where(o => o.CreatedAt <= endDate.Value).ToList();

            return operations
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
    }

}
