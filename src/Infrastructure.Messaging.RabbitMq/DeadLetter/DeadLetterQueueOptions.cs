using System.ComponentModel.DataAnnotations;

namespace Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

/// <summary>
/// Configuration options for dead-letter queue handling
/// </summary>
public sealed class DeadLetterQueueOptions
{
    public const string SectionName = "DeadLetterQueue";

    /// <summary>
    /// Name of the dead-letter exchange
    /// </summary>
    [Required]
    public string DeadLetterExchange { get; init; } = "netemplate.dlx";

    /// <summary>
    /// Name of the dead-letter queue
    /// </summary>
    [Required]
    public string DeadLetterQueue { get; init; } = "netemplate.dlq";

    /// <summary>
    /// Number of messages in DLQ before triggering operator alert
    /// </summary>
    [Range(1, 10000)]
    public int AlertThreshold { get; init; } = 100;

    /// <summary>
    /// Message time-to-live in seconds (0 = infinite)
    /// </summary>
    [Range(0, 86400)]
    public int MessageTtlSeconds { get; init; } = 86400; // 24 hours

    /// <summary>
    /// Enable automatic DLQ monitoring
    /// </summary>
    public bool EnableMonitoring { get; init; } = true;

    /// <summary>
    /// Maximum number of retries before dead-lettering
    /// </summary>
    [Range(0, 10)]
    public int MaxRetries { get; init; } = 3;
}
