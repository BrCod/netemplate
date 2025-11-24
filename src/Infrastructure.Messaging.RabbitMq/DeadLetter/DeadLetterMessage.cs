namespace Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

/// <summary>
/// Represents a message that failed processing and was sent to the dead-letter queue
/// </summary>
public sealed record DeadLetterMessage
{
    /// <summary>
    /// Original routing key/topic the message was published to
    /// </summary>
    public required string OriginalRoutingKey { get; init; }

    /// <summary>
    /// Original exchange the message was published to
    /// </summary>
    public required string OriginalExchange { get; init; }

    /// <summary>
    /// The raw message body that failed processing
    /// </summary>
    public required byte[] Body { get; init; }

    /// <summary>
    /// Reason the message was dead-lettered (e.g., "rejected", "expired", "max-retries-exceeded")
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Number of times this message was attempted before dead-lettering
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Exception message if the failure was due to an exception
    /// </summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>
    /// Exception stack trace if available
    /// </summary>
    public string? ExceptionStackTrace { get; init; }

    /// <summary>
    /// Timestamp when the message was dead-lettered
    /// </summary>
    public DateTimeOffset DeadLetteredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Original message headers
    /// </summary>
    public IDictionary<string, object?>? Headers { get; init; }
}
