namespace Netemplate.Infrastructure.FeatureFlags.DTOs;

/// <summary>
/// Audit entry for feature flag changes (query result)
/// </summary>
public sealed record FeatureFlagAuditEntry
{
    public required Guid Id { get; init; }
    public required Guid FeatureFlagId { get; init; }
    public required string FeatureFlagName { get; init; }
    public required bool PreviousValue { get; init; }
    public required bool NewValue { get; init; }
    public required DateTime ChangedAt { get; init; }
    public string? ChangedBy { get; init; }
    public string? Reason { get; init; }
    public string? CorrelationId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
