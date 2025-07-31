using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TechScriptAid.Core.Interfaces.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TechScriptAid.AI.Services
{
    /// <summary>
    /// Redis-based caching service for AI responses
    /// </summary>
    public class AICacheService : IAICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<AICacheService> _logger; // Fix: Add the missing _logger field

        private const string CacheKeyPrefix = "ai_cache:";

        public AICacheService(
            IDistributedCache cache,
            ILogger<AICacheService> logger) // Fix: Add ILogger<AICacheService> parameter
        {
            _cache = cache;
            _logger = logger; // Fix: Initialize _logger
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var fullKey = GetFullKey(key);
                var cachedData = await _cache.GetStringAsync(fullKey);

                if (string.IsNullOrEmpty(cachedData))
                    return null;

                return JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache with key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var fullKey = GetFullKey(key);
                var serializedData = JsonSerializer.Serialize(value);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(15)
                };

                await _cache.SetStringAsync(fullKey, serializedData, options);
                _logger.LogDebug("Cached data with key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache with key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var fullKey = GetFullKey(key);
                await _cache.RemoveAsync(fullKey);
                _logger.LogDebug("Removed cache with key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache with key: {Key}", key);
            }
        }

        public string GenerateCacheKey(string operation, object request)
        {
            var requestJson = JsonSerializer.Serialize(request);
            var hash = ComputeHash(requestJson);
            return $"{operation}:{hash}";
        }

        private string GetFullKey(string key)
        {
            return $"{CacheKeyPrefix}{key}";
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
