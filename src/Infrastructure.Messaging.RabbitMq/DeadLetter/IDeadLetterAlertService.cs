namespace Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

/// <summary>
/// Interface for alerting operators about dead-letter queue events
/// </summary>
public interface IDeadLetterAlertService
{
    /// <summary>
    /// Alert operators about a message being dead-lettered
    /// </summary>
    Task AlertAsync(DeadLetterMessage message, CancellationToken ct = default);

    /// <summary>
    /// Alert operators about DLQ threshold being exceeded
    /// </summary>
    Task AlertThresholdExceededAsync(int messageCount, int threshold, CancellationToken ct = default);
}
