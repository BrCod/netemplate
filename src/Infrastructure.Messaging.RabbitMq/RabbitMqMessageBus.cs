using Netemplate.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Netemplate.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqMessageBus : IMessageBus
{
    private readonly IConnectionFactory _connectionFactory;

    public RabbitMqMessageBus(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        
        await channel.ExchangeDeclareAsync(exchange: "netemplate", type: ExchangeType.Topic, durable: true, cancellationToken: ct);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: "netemplate",
            routingKey: topic,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }

    public async Task SubscribeAsync(string topic, Func<byte[], CancellationToken, Task> handler, CancellationToken ct = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        
        await channel.ExchangeDeclareAsync(exchange: "netemplate", type: ExchangeType.Topic, durable: true, cancellationToken: ct);

        var queueDeclareResult = await channel.QueueDeclareAsync(cancellationToken: ct);
        await channel.QueueBindAsync(queue: queueDeclareResult.QueueName, exchange: "netemplate", routingKey: topic, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            await handler(ea.Body.ToArray(), ct);
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: ct);
        };

        await channel.BasicConsumeAsync(queue: queueDeclareResult.QueueName, autoAck: false, consumer: consumer, cancellationToken: ct);
    }
}
