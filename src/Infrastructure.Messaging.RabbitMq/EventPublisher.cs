using Netemplate.Application.Interfaces;

namespace Netemplate.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Publishes domain events using the message bus.
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly IMessageBus _messageBus;

    public EventPublisher(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public Task PublishAsync<T>(T @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        
        // Determine the routing key based on the event type name
        var routingKey = typeof(T).Name.ToLowerInvariant();
        
        return _messageBus.PublishAsync(routingKey, @event, ct);
    }
}
