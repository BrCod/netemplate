using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Policies.Config;

namespace Netemplate.Infrastructure.Messaging.RabbitMq;

public sealed class ResilientMessageBus : IMessageBus
{
    private readonly IMessageBus _inner;
    private readonly MessagingPolicies _policies;

    public ResilientMessageBus(IMessageBus inner, IResiliencePolicyRegistry registry)
    {
        _inner = inner;
        _policies = registry.Messaging;
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
    {
        await _policies.Retry.ExecuteAsync(async () =>
            await _policies.CircuitBreaker.ExecuteAsync(async () =>
                await _policies.Bulkhead.ExecuteAsync(async () =>
                    await _policies.Timeout.ExecuteAsync(async _ => await _inner.PublishAsync(topic, message, ct), ct))));
    }

    public async Task SubscribeAsync(string topic, Func<byte[], CancellationToken, Task> handler, CancellationToken ct = default)
    {
        // Subscription setup is less frequent; still protect with circuit breaker.
        await _policies.CircuitBreaker.ExecuteAsync(async () =>
            await _policies.Bulkhead.ExecuteAsync(async () =>
                await _inner.SubscribeAsync(topic, handler, ct)));
    }
}