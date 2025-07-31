using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.DTOs.AI
{
    public class AIUsageStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public long TotalTokensUsed { get; set; }
        public decimal TotalCost { get; set; }
        public Dictionary<string, int> RequestsByOperation { get; set; } = new();
        public Dictionary<string, long> TokensByOperation { get; set; } = new();
        public double AverageResponseTimeMs { get; set; }
    }
}
