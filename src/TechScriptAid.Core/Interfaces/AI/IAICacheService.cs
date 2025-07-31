using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechScriptAid.Core.Interfaces.AI
{
    /// <summary>
    /// Caching service for AI responses
    /// </summary>
    public interface IAICacheService
    {
        /// <summary>
        /// Gets a cached AI response
        /// </summary>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Sets a cached AI response
        /// </summary>
        //Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;  // Note the nullable TimeSpan

        /// <summary>
        /// Removes a cached item
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Generates a cache key for AI requests
        /// </summary>
        string GenerateCacheKey(string operation, object request);
    }

}
