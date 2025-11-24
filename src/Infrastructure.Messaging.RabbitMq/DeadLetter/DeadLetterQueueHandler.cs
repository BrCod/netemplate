using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

/// <summary>
/// Handles dead-letter queue setup, monitoring, and alerting for RabbitMQ
/// </summary>
public sealed class DeadLetterQueueHandler : IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IDeadLetterAlertService _alertService;
    private readonly ILogger<DeadLetterQueueHandler> _logger;
    private readonly DeadLetterQueueOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    public DeadLetterQueueHandler(
        IConnectionFactory connectionFactory,
        IDeadLetterAlertService alertService,
        ILogger<DeadLetterQueueHandler> logger,
        DeadLetterQueueOptions options)
    {
        _connectionFactory = connectionFactory;
        _alertService = alertService;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Declare dead-letter exchange and queue with appropriate bindings
    /// </summary>
    public async Task SetupDeadLetterInfrastructureAsync(CancellationToken ct = default)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        // Declare dead-letter exchange
        await _channel.ExchangeDeclareAsync(
            exchange: _options.DeadLetterExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        // Declare dead-letter queue with message TTL if configured
        var queueArgs = new Dictionary<string, object?>();
        if (_options.MessageTtlSeconds > 0)
        {
            queueArgs["x-message-ttl"] = _options.MessageTtlSeconds * 1000;
        }

        await _channel.QueueDeclareAsync(
            queue: _options.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: ct);

        // Bind dead-letter queue to dead-letter exchange with wildcard routing
        await _channel.QueueBindAsync(
            queue: _options.DeadLetterQueue,
            exchange: _options.DeadLetterExchange,
            routingKey: "#",
            cancellationToken: ct);

        _logger.LogInformation(
            "Dead-letter infrastructure setup completed. Exchange={Exchange}, Queue={Queue}",
            _options.DeadLetterExchange,
            _options.DeadLetterQueue);
    }

    /// <summary>
    /// Configure a queue with dead-letter exchange routing
    /// </summary>
    public static Dictionary<string, object?> GetDeadLetterQueueArguments(string deadLetterExchange, int maxRetries = 3)
    {
        return new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = deadLetterExchange,
            ["x-dead-letter-routing-key"] = "dlq",
            ["x-max-length"] = maxRetries // Limit queue length to trigger DLQ sooner
        };
    }

    /// <summary>
    /// Start monitoring the dead-letter queue and trigger alerts
    /// </summary>
    public async Task StartMonitoringAsync(CancellationToken ct = default)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Dead-letter infrastructure must be set up before monitoring");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var deadLetterMessage = ParseDeadLetterMessage(ea);
                
                _logger.LogWarning(
                    "Processing dead-lettered message. RoutingKey={RoutingKey}, Reason={Reason}, RetryCount={RetryCount}",
                    deadLetterMessage.OriginalRoutingKey,
                    deadLetterMessage.Reason,
                    deadLetterMessage.RetryCount);

                // Alert operators
                await _alertService.AlertAsync(deadLetterMessage, ct);

                // Check if threshold exceeded
                var queueInfo = await _channel.QueueDeclarePassiveAsync(_options.DeadLetterQueue, ct);
                if (queueInfo.MessageCount >= _options.AlertThreshold)
                {
                    await _alertService.AlertThresholdExceededAsync(
                        (int)queueInfo.MessageCount,
                        _options.AlertThreshold,
                        ct);
                }

                // Acknowledge the message (removes it from DLQ after processing/alerting)
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dead-letter message. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                // Negative acknowledgement - requeue for retry
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.DeadLetterQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation("Dead-letter queue monitoring started. Queue={Queue}", _options.DeadLetterQueue);
    }

    /// <summary>
    /// Get current dead-letter queue statistics
    /// </summary>
    public async Task<DeadLetterQueueStats> GetStatsAsync(CancellationToken ct = default)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Dead-letter infrastructure must be set up before getting stats");
        }

        var queueInfo = await _channel.QueueDeclarePassiveAsync(_options.DeadLetterQueue, ct);

        return new DeadLetterQueueStats
        {
            MessageCount = (int)queueInfo.MessageCount,
            ConsumerCount = (int)queueInfo.ConsumerCount,
            QueueName = _options.DeadLetterQueue,
            IsThresholdExceeded = queueInfo.MessageCount >= _options.AlertThreshold
        };
    }

    private static DeadLetterMessage ParseDeadLetterMessage(BasicDeliverEventArgs ea)
    {
        var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>();
        
        // Extract death reason and retry count from RabbitMQ headers
        var deathHeader = headers.TryGetValue("x-death", out var deathObj) ? deathObj : null;
        var reason = "unknown";
        var retryCount = 0;
        var originalRoutingKey = ea.RoutingKey;
        var originalExchange = ea.Exchange;

        if (deathHeader is List<object> deaths && deaths.Count > 0 && deaths[0] is Dictionary<string, object> death)
        {
            if (death.TryGetValue("reason", out var reasonObj) && reasonObj is byte[] reasonBytes)
            {
                reason = Encoding.UTF8.GetString(reasonBytes);
            }
            if (death.TryGetValue("count", out var countObj))
            {
                retryCount = Convert.ToInt32(countObj);
            }
            if (death.TryGetValue("routing-keys", out var keysObj) && keysObj is List<object> keys && keys.Count > 0)
            {
                originalRoutingKey = keys[0]?.ToString() ?? originalRoutingKey;
            }
            if (death.TryGetValue("exchange", out var exchangeObj) && exchangeObj is byte[] exchangeBytes)
            {
                originalExchange = Encoding.UTF8.GetString(exchangeBytes);
            }
        }

        var correlationId = ea.BasicProperties.CorrelationId;

        return new DeadLetterMessage
        {
            OriginalRoutingKey = originalRoutingKey,
            OriginalExchange = originalExchange,
            Body = ea.Body.ToArray(),
            Reason = reason,
            RetryCount = retryCount,
            CorrelationId = correlationId,
            Headers = headers
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}

/// <summary>
/// Statistics about the dead-letter queue
/// </summary>
public sealed record DeadLetterQueueStats
{
    public required string QueueName { get; init; }
    public required int MessageCount { get; init; }
    public required int ConsumerCount { get; init; }
    public required bool IsThresholdExceeded { get; init; }
}
