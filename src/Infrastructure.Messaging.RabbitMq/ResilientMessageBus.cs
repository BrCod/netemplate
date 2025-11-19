using Netemplate.Application.Interfaces;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Netemplate.Infrastructure.Messaging.RabbitMq;

public sealed class ResilientMessageBus : IMessageBus
{
    private readonly IMessageBus _inner;
    private readonly AsyncRetryPolicy _retry;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private readonly AsyncTimeoutPolicy _timeout;

    public ResilientMessageBus(IMessageBus inner)
    {
        _inner = inner;
        _retry = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(300 * attempt));
        _circuitBreaker = Policy.Handle<Exception>()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        _timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(10));
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
    {
        await _retry.ExecuteAsync(async () =>
            await _circuitBreaker.ExecuteAsync(async () =>
                await _timeout.ExecuteAsync(async _ => await _inner.PublishAsync(topic, message, ct), ct)));
    }

    public async Task SubscribeAsync(string topic, Func<byte[], CancellationToken, Task> handler, CancellationToken ct = default)
    {
        // Subscription setup is less frequent; still protect with circuit breaker.
        await _circuitBreaker.ExecuteAsync(async () =>
            await _inner.SubscribeAsync(topic, handler, ct));
    }
}