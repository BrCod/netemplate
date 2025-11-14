using System.Collections.Concurrent;

namespace Application.Services.FeatureFlags
{
    /// <summary>
    /// In-memory feature flag service abstraction.
    /// </summary>
    public class FeatureFlagService
    {
        private readonly ConcurrentDictionary<string, bool> _flags = new();
        /// <summary>Check if a feature flag is enabled.</summary>
        public bool IsEnabled(string flag) => _flags.TryGetValue(flag, out var enabled) && enabled;
        /// <summary>Set the value of a feature flag.</summary>
        public void SetFlag(string flag, bool enabled) => _flags[flag] = enabled;
    }
}
