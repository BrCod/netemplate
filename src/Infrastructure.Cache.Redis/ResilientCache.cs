using Netemplate.Application.Interfaces;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Netemplate.Infrastructure.Cache.Redis;

public sealed class ResilientCache : ICache
{
    private readonly ICache _inner;
    private readonly AsyncRetryPolicy _retry;
    private readonly AsyncTimeoutPolicy _timeout;

    public ResilientCache(ICache inner)
    {
        _inner = inner;
        _retry = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));
        _timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(5));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return await _retry.ExecuteAsync(async () =>
            await _timeout.ExecuteAsync(async _ => await _inner.GetAsync<T>(key, ct), ct));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await _retry.ExecuteAsync(async () =>
            await _timeout.ExecuteAsync(async _ => { await _inner.SetAsync(key, value, ttl, ct); }, ct));
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _retry.ExecuteAsync(async () =>
            await _timeout.ExecuteAsync(async _ => { await _inner.RemoveAsync(key, ct); }, ct));
    }
}