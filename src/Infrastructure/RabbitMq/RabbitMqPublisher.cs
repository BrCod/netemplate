using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.RabbitMq
{
    /// <summary>
    /// Publishes events to RabbitMQ.
    /// </summary>
    public class RabbitMqPublisher : IEventPublisher
    {
        private readonly RabbitMQ.Client.IChannel _channel;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMqPublisher"/> class.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        public RabbitMqPublisher(IChannel channel)
        {
            _channel = channel;
        }
        
        /// <summary>
        /// Publishes an event to RabbitMQ.
        /// </summary>
        /// <param name="event">The event to publish.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task PublishEventAsync(object @event)
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
            var props = new BasicProperties();
            props.ContentType = "application/json";
            await _channel.BasicPublishAsync(exchange: "events", routingKey: envelope.EventType, mandatory: false, basicProperties: props, body: Encoding.UTF8.GetBytes(body));
        }
    }
}
