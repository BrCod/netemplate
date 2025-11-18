namespace Netemplate.Application.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default);
    Task SubscribeAsync(string topic, Func<byte[], CancellationToken, Task> handler, CancellationToken ct = default);
}
