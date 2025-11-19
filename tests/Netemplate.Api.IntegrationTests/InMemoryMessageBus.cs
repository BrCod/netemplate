using Netemplate.Application.Interfaces;

namespace Netemplate.Api.IntegrationTests;

public sealed class InMemoryMessageBus : IMessageBus
{
    private readonly Dictionary<string, List<Func<byte[], CancellationToken, Task>>> _subscriptions = new();

    public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
    {
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message!);
        if (_subscriptions.TryGetValue(topic, out var handlers))
        {
            var tasks = handlers.Select(h => h(bytes, ct));
            return Task.WhenAll(tasks);
        }
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(string topic, Func<byte[], CancellationToken, Task> handler, CancellationToken ct = default)
    {
        if (!_subscriptions.TryGetValue(topic, out var handlers))
        {
            handlers = new List<Func<byte[], CancellationToken, Task>>();
            _subscriptions[topic] = handlers;
        }

        handlers.Add(handler);
        return Task.CompletedTask;
    }
}
