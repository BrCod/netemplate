namespace Netemplate.Infrastructure.FeatureFlags.Entities;

/// <summary>
/// Audit trail entry for feature flag changes
/// </summary>
public sealed class FeatureFlagAuditLog
{
    public Guid Id { get; set; }
    public Guid FeatureFlagId { get; set; }
    public string FeatureFlagName { get; set; } = string.Empty;
    public bool PreviousValue { get; set; }
    public bool NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? Reason { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation property
    public FeatureFlag? FeatureFlag { get; set; }
}
