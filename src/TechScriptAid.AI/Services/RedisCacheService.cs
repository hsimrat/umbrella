using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.AI.Services
{
    public class RedisCacheService : IAICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public string GenerateCacheKey(string operation, object request)
        {
            var requestJson = JsonSerializer.Serialize(request);
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(requestJson));
            return $"ai:{operation}:{Convert.ToBase64String(hash)}";
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            var data = await _cache.GetStringAsync(key);
            if (data == null)
            {
                _logger.LogInformation("Cache miss for key: {CacheKey}", key);
                return null;
            }

            _logger.LogInformation("Cache hit for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
                options.AbsoluteExpirationRelativeToNow = expiration.Value;

            var data = JsonSerializer.Serialize(value);
            _logger.LogInformation("Cache set for key: {CacheKey}", key);
            await _cache.SetStringAsync(key, data, options);
          
        }

        public async Task RemoveAsync(string key)
        {
            _logger.LogInformation("Removing cache for key: {CacheKey}", key);
            await _cache.RemoveAsync(key);
        }
    }
}