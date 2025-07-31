using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.AI.Services
{
    public class AIRateLimiter : IAIRateLimiter
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<AIRateLimiter> _logger;
        private readonly AIConfiguration _configuration;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _userSemaphores;

        public AIRateLimiter(
            IMemoryCache cache,
            ILogger<AIRateLimiter> logger,
            IOptions<AIConfiguration> configuration)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration.Value;
            // Use fully qualified name to avoid ambiguity
            _userSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public async Task<bool> CheckRateLimitAsync(string userId, CancellationToken cancellationToken = default)
        {
            var key = GetRateLimitKey(userId);
            var status = await GetStatusAsync(userId);

            if (status.RequestsRemaining <= 0)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId}. Resets at {ResetsAt}",
                    userId, status.ResetsAt);
                return false;
            }

            // Use the RateLimits property from AIConfiguration
            var semaphore = _userSemaphores.GetOrAdd(userId,
                _ => new SemaphoreSlim(_configuration.RateLimits.ConcurrentRequests));

            if (!await semaphore.WaitAsync(0, cancellationToken))
            {
                _logger.LogWarning("Concurrent request limit exceeded for user {UserId}", userId);
                return false;
            }

            _ = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => semaphore.Release());

            return true;
        }

        public async Task RecordRequestAsync(string userId, int tokens)
        {
            var requestKey = GetRequestCountKey(userId);
            var tokenKey = GetTokenCountKey(userId);
            var now = DateTime.UtcNow;

            var requestCount = _cache.Get<RequestCounter>(requestKey) ?? new RequestCounter { Count = 0, WindowStart = now };
            var tokenCount = _cache.Get<TokenCounter>(tokenKey) ?? new TokenCounter { Count = 0, WindowStart = now };

            if (now - requestCount.WindowStart > TimeSpan.FromMinutes(1))
            {
                requestCount = new RequestCounter { Count = 0, WindowStart = now };
            }

            if (now - tokenCount.WindowStart > TimeSpan.FromMinutes(1))
            {
                tokenCount = new TokenCounter { Count = 0, WindowStart = now };
            }

            requestCount.Count++;
            tokenCount.Count += tokens;

            _cache.Set(requestKey, requestCount, TimeSpan.FromMinutes(2));
            _cache.Set(tokenKey, tokenCount, TimeSpan.FromMinutes(2));

            _logger.LogInformation("Recorded request for user {UserId}: {Tokens} tokens", userId, tokens);

            await Task.CompletedTask;
        }

        public async Task<RateLimitStatus> GetStatusAsync(string userId)
        {
            var requestKey = GetRequestCountKey(userId);
            var tokenKey = GetTokenCountKey(userId);
            var now = DateTime.UtcNow;

            var requestCount = _cache.Get<RequestCounter>(requestKey) ?? new RequestCounter { Count = 0, WindowStart = now };
            var tokenCount = _cache.Get<TokenCounter>(tokenKey) ?? new TokenCounter { Count = 0, WindowStart = now };

            var requestsRemaining = Math.Max(0, _configuration.RateLimits.RequestsPerMinute - requestCount.Count);
            var tokensRemaining = Math.Max(0,
                (_configuration.RateLimits.RequestsPerHour * 1500) - tokenCount.Count);

            var resetTime = requestCount.WindowStart.AddMinutes(1);
            if (now - requestCount.WindowStart > TimeSpan.FromMinutes(1))
            {
                requestsRemaining = _configuration.RateLimits.RequestsPerMinute;
                resetTime = now.AddMinutes(1);
            }

            return await Task.FromResult(new RateLimitStatus
            {
                RequestsRemaining = requestsRemaining,
                TokensRemaining = tokensRemaining,
                ResetsAt = resetTime
            });
        }

        private string GetRateLimitKey(string userId) => $"rate_limit:{userId}";
        private string GetRequestCountKey(string userId) => $"request_count:{userId}";
        private string GetTokenCountKey(string userId) => $"token_count:{userId}";

        private class RequestCounter
        {
            public int Count { get; set; }
            public DateTime WindowStart { get; set; }
        }

        private class TokenCounter
        {
            public int Count { get; set; }
            public DateTime WindowStart { get; set; }
        }
    }
}

/// <summary>
/// Implements rate limiting for AI operations
/// </summary>
//public class AIRateLimiter : IAIRateLimiter
//{
//    private readonly IMemoryCache _cache;
//    private readonly IUnitOfWork _unitOfWork;
//    private readonly ILogger<AIRateLimiter> _logger;
//    private readonly RateLimitConfiguration _settings;

//    public AIRateLimiter(
//        IMemoryCache cache,
//        IUnitOfWork unitOfWork,
//        ILogger<AIRateLimiter> logger,
//        RateLimitConfiguration settings)
//    {
//        _cache = cache;
//        _unitOfWork = unitOfWork;
//        _logger = logger;
//        _settings = settings;
//    }

//    public async Task<bool> TryAcquireAsync(string userId, string resource, int tokens)
//    {
//        var key = $"ratelimit:{userId}:{resource}";
//        var window = GetCurrentWindow();

//        // Get or create tracker
//        var tracker = await _cache.GetOrCreateAsync(key, async entry =>
//        {
//            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

//            var repository = _unitOfWork.Repository<AIRateLimitTracker>();
//            var existing = (await repository.GetAllAsync())
//                .FirstOrDefault(t => t.UserId == userId &&
//                                   t.Resource == resource &&
//                                   t.WindowStart == window);

//            return existing ?? new AIRateLimitTracker
//            {
//                UserId = userId,
//                Resource = resource,
//                WindowStart = window,
//                RequestCount = 0,
//                TokenCount = 0
//            };
//        });

//        // Check limits
//        if (tracker!.RequestCount >= _settings.RequestsPerMinute)
//        {
//            _logger.LogWarning("Rate limit exceeded for user {UserId} on resource {Resource}", userId, resource);
//            tracker.IsThrottled = true;
//            tracker.ThrottledUntil = window.AddMinutes(1);
//            await UpdateTrackerAsync(tracker);
//            return false;
//        }

//        if (tracker.TokenCount + tokens > _settings.TokensPerMinute)
//        {
//            _logger.LogWarning("Token limit exceeded for user {UserId} on resource {Resource}", userId, resource);
//            tracker.IsThrottled = true;
//            tracker.ThrottledUntil = window.AddMinutes(1);
//            await UpdateTrackerAsync(tracker);
//            return false;
//        }

//        // Update counts
//        tracker.RequestCount++;
//        tracker.TokenCount += tokens;
//        tracker.LastRequestAt = DateTime.UtcNow;

//        await UpdateTrackerAsync(tracker);
//        _cache.Set(key, tracker, TimeSpan.FromMinutes(1));

//        return true;
//    }

//    public async Task ReleaseAsync(string userId, string resource, int tokensUsed)
//    {
//        var key = $"ratelimit:{userId}:{resource}";
//        if (_cache.TryGetValue<AIRateLimitTracker>(key, out var tracker))
//        {
//            // Update actual token usage
//            tracker.TokenCount = Math.Max(0, tracker.TokenCount - tokensUsed);
//            await UpdateTrackerAsync(tracker);
//        }
//    }

//    private DateTime GetCurrentWindow()
//    {
//        var now = DateTime.UtcNow;
//        return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
//    }

//    private async Task UpdateTrackerAsync(AIRateLimitTracker tracker)
//    {
//        var repository = _unitOfWork.Repository<AIRateLimitTracker>();

//        if (tracker.Id == 0)
//        {
//            await repository.AddAsync(tracker);
//        }
//        else
//        {
//            repository.Update(tracker);
//        }

//        await _unitOfWork.CompleteAsync();
//    }
//}
