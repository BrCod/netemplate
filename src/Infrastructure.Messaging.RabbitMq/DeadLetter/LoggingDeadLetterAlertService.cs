using Microsoft.Extensions.Logging;

namespace Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

/// <summary>
/// Default implementation that logs dead-letter events with structured logging
/// </summary>
public sealed class LoggingDeadLetterAlertService : IDeadLetterAlertService
{
    private readonly ILogger<LoggingDeadLetterAlertService> _logger;

    public LoggingDeadLetterAlertService(ILogger<LoggingDeadLetterAlertService> logger)
    {
        _logger = logger;
    }

    public Task AlertAsync(DeadLetterMessage message, CancellationToken ct = default)
    {
        _logger.LogError(
            "Message dead-lettered. RoutingKey={RoutingKey}, Exchange={Exchange}, Reason={Reason}, " +
            "RetryCount={RetryCount}, CorrelationId={CorrelationId}, Exception={ExceptionMessage}",
            message.OriginalRoutingKey,
            message.OriginalExchange,
            message.Reason,
            message.RetryCount,
            message.CorrelationId,
            message.ExceptionMessage);

        return Task.CompletedTask;
    }

    public Task AlertThresholdExceededAsync(int messageCount, int threshold, CancellationToken ct = default)
    {
        _logger.LogCritical(
            "Dead-letter queue threshold exceeded. MessageCount={MessageCount}, Threshold={Threshold}. " +
            "Immediate operator attention required.",
            messageCount,
            threshold);

        return Task.CompletedTask;
    }
}
