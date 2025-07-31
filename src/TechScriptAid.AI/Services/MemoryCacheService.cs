using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.AI.Services
{
    public class MemoryCacheService : IAICacheService
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;

            // Add this logging
            Console.WriteLine("⚠️ WARNING: Using MemoryCacheService - Redis is not connected!");
            Console.WriteLine("⚠️ Cache data will be lost on application restart!");
        }

        public string GenerateCacheKey(string operation, object request)
        {
            var requestJson = JsonSerializer.Serialize(request);
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(requestJson));
            return $"ai:{operation}:{Convert.ToBase64String(hash)}";
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            return await Task.FromResult(_cache.Get<T>(key));
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (expiration.HasValue)
                _cache.Set(key, value, expiration.Value);
            else
                _cache.Set(key, value);

            await Task.CompletedTask;
        }

        public async Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            await Task.CompletedTask;
        }
    }
}
