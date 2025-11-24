namespace Netemplate.Infrastructure.FeatureFlags.DTOs;

/// <summary>
/// Request to change a feature flag state
/// </summary>
public sealed record FeatureFlagChangeRequest
{
    public required string FeatureName { get; init; }
    public required bool IsEnabled { get; init; }
    public string? Reason { get; init; }
    public string? ChangedBy { get; init; }
    public string? CorrelationId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
