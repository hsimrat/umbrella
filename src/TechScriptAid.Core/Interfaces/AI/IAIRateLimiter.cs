using System.Threading;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Interfaces.AI
{
    public interface IAIRateLimiter
    {
        Task<bool> CheckRateLimitAsync(string userId, CancellationToken cancellationToken = default);
        Task RecordRequestAsync(string userId, int tokens);
        Task<RateLimitStatus> GetStatusAsync(string userId);
    }

    public class RateLimitStatus
    {
        public int RequestsRemaining { get; set; }
        public int TokensRemaining { get; set; }
        public DateTime ResetsAt { get; set; }
    }
}