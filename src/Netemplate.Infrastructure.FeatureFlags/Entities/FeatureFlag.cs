namespace Netemplate.Infrastructure.FeatureFlags.Entities;

/// <summary>
/// Represents a feature flag with its current state
/// </summary>
public sealed class FeatureFlag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Tags { get; set; } // JSON array of tags for categorization
}
