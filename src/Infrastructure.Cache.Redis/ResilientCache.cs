using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Policies.Config;

namespace Netemplate.Infrastructure.Cache.Redis;

public sealed class ResilientCache : ICache
{
    private readonly ICache _inner;
    private readonly CachePolicies _policies;

    public ResilientCache(ICache inner, IResiliencePolicyRegistry registry)
    {
        _inner = inner;
        _policies = registry.Cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return await _policies.Retry.ExecuteAsync(async () =>
            await _policies.Bulkhead.ExecuteAsync(async () =>
                await _policies.Timeout.ExecuteAsync(async _ => await _inner.GetAsync<T>(key, ct), ct)));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await _policies.Retry.ExecuteAsync(async () =>
            await _policies.Bulkhead.ExecuteAsync(async () =>
                await _policies.Timeout.ExecuteAsync(async _ => { await _inner.SetAsync(key, value, ttl, ct); }, ct)));
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _policies.Retry.ExecuteAsync(async () =>
            await _policies.Bulkhead.ExecuteAsync(async () =>
                await _policies.Timeout.ExecuteAsync(async _ => { await _inner.RemoveAsync(key, ct); }, ct)));
    }
}