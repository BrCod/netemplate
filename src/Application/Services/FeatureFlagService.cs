namespace Netemplate.Application.Services;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);
}

public sealed class InMemoryFeatureFlagService : IFeatureFlagService
{
    private readonly Dictionary<string, bool> _flags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProductCaching"] = true,
        ["EventPublishing"] = true,
        ["RateLimiting"] = true
    };

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        return Task.FromResult(_flags.TryGetValue(featureName, out var enabled) && enabled);
    }

    public void SetFlag(string featureName, bool enabled)
    {
        _flags[featureName] = enabled;
    }
}
