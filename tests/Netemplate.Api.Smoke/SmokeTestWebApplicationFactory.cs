using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Netemplate.Application.Interfaces;

namespace Netemplate.Api.Smoke;

/// <summary>
/// Custom WebApplicationFactory for smoke tests that replaces external dependencies
/// with in-memory implementations to avoid infrastructure requirements.
/// </summary>
public class SmokeTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove Redis-related services
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(IConnectionMultiplexer) ||
                           d.ServiceType == typeof(ICache) ||
                           d.ServiceType == typeof(IMessageBus) ||
                           d.ImplementationType?.Name.Contains("Redis") == true ||
                           d.ImplementationType?.Name.Contains("RabbitMq") == true ||
                           d.ImplementationType?.Name.Contains("Resilient") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Register in-memory replacements
            services.AddSingleton<ICache, InMemoryCache>();
            services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        });
    }
}

/// <summary>
/// Simple in-memory cache implementation for smoke tests.
/// </summary>
public class InMemoryCache : ICache
{
    private readonly Dictionary<string, object> _cache = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult(value is T typed ? typed : default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        _cache[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple in-memory message bus implementation for smoke tests.
/// </summary>
public class InMemoryMessageBus : IMessageBus
{
    public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
    {
        // Simply no-op for smoke tests
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(string topic, Func<byte[], CancellationToken, Task> handler, CancellationToken ct = default)
    {
        // Simply no-op for smoke tests
        return Task.CompletedTask;
    }
}
