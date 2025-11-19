using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Netemplate.Api.Health;

public static class HealthCheckConfiguration
{
    public static HealthCheckOptions CreateDetailedOptions(bool includeExceptions = false)
    {
        return new HealthCheckOptions
        {
            ResponseWriter = DetailedHealthCheckWriter.WriteResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            AllowCachingResponses = false
        };
    }

    public static HealthCheckOptions CreateLivenessOptions()
    {
        return new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("liveness"),
            AllowCachingResponses = false
        };
    }

    public static HealthCheckOptions CreateReadinessOptions()
    {
        return new HealthCheckOptions
        {
            Predicate = _ => true, // All checks for readiness
            ResponseWriter = DetailedHealthCheckWriter.WriteResponse,
            AllowCachingResponses = false
        };
    }
}
