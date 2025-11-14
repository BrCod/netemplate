using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ;
using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.RabbitMq
{
    public class RabbitMqPublisher : IEventPublisher
    {
        private readonly RabbitMQ.Client.IChannel _channel;
        public RabbitMqPublisher(IChannel channel)
        {
            _channel = channel;
        }
        public Task PublishEventAsync(object @event)
        {
            var envelope = new MessageEnvelope
            {
                CorrelationId = Guid.NewGuid(),
                SchemaVersion = 1,
                Timestamp = DateTime.UtcNow,
                EventType = @event.GetType().Name,
                Payload = @event
            };
            var body = JsonSerializer.Serialize(envelope);
            var props = _channel..CreateBasicProperties();
            props.ContentType = "application/json";
            _channel.BasicPublishAsync(exchange: "events", routingKey: envelope.EventType, basicProperties: props, body: Encoding.UTF8.GetBytes(body));
            return Task.CompletedTask;
        }
    }
}
