using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Interface for cache adapter operations.
    /// </summary>
    public interface ICache
    {
        /// <summary>Get cached value by key.</summary>
        Task<T?> GetAsync<T>(string key);
        /// <summary>Set value in cache with optional TTL.</summary>
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        /// <summary>Remove value from cache by key.</summary>
        Task RemoveAsync(string key);
    }
}
