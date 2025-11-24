using System.ComponentModel.DataAnnotations;

namespace Netemplate.Infrastructure.Policies.Config;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public CachePolicyOptions Cache { get; init; } = new();
    public MessagingPolicyOptions Messaging { get; init; } = new();
}

public sealed class CachePolicyOptions
{
    [Range(0, 10)] public int RetryCount { get; init; } = 3;
    [Range(1, 30000)] public int BaseRetryDelayMs { get; init; } = 200;
    [Range(1, 120)] public int TimeoutSeconds { get; init; } = 5;
    [Range(1, 1000)] public int BulkheadMaxConcurrency { get; init; } = 50;
    [Range(0, 5000)] public int BulkheadQueueLimit { get; init; } = 200;
}

public sealed class MessagingPolicyOptions
{
    [Range(0, 10)] public int RetryCount { get; init; } = 3;
    [Range(1, 30000)] public int BaseRetryDelayMs { get; init; } = 300;
    [Range(1, 300)] public int TimeoutSeconds { get; init; } = 10;
    [Range(1, 100)] public int CircuitBreakerFailures { get; init; } = 5;
    [Range(1, 600)] public int CircuitBreakerDurationSeconds { get; init; } = 30;
    [Range(1, 1000)] public int BulkheadMaxConcurrency { get; init; } = 20;
    [Range(0, 5000)] public int BulkheadQueueLimit { get; init; } = 200;
}
