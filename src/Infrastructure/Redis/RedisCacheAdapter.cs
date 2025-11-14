using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Application.Interfaces;

namespace Infrastructure.Redis
{
    /// <summary>
    /// Provides Redis-based caching services.
    /// </summary>
    public class RedisCacheAdapter : ICache
    {
        private readonly IDatabase _db;
        private readonly string _tenantPrefix;
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheAdapter"/> class.
        /// </summary>
        /// <param name="redis">The Redis connection multiplexer.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        public RedisCacheAdapter(IConnectionMultiplexer redis, string tenantId)
        {
            _db = redis.GetDatabase();
            _tenantPrefix = tenantId ?? "default";
        }
        private string GetKey(string key) => $"{_tenantPrefix}:{key}";
        /// <summary>
        /// Retrieves a cached value asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <returns>The cached value, or default if not found.</returns>
        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(GetKey(key));
            return value.HasValue ? System.Text.Json.JsonSerializer.Deserialize<T>(value!) : default;
        }
        /// <summary>
        /// Sets a value in the cache asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="ttl">The time-to-live for the cache entry.</param>
        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            await _db.StringSetAsync(GetKey(key), json, ttl);
        }
        /// <summary>
        /// Removes a value from the cache asynchronously.
        /// </summary>
        /// <param name="key">The cache key.</param>
        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(GetKey(key));
        }
    }
}
