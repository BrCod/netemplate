using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Netemplate.Api.Health;

/// <summary>
/// Custom health check for application-level readiness
/// </summary>
public sealed class ApplicationReadinessCheck : IHealthCheck
{
    private readonly ILogger<ApplicationReadinessCheck> _logger;
    private bool _isReady = false;

    public ApplicationReadinessCheck(ILogger<ApplicationReadinessCheck> logger)
    {
        _logger = logger;
    }

    public void MarkAsReady()
    {
        _isReady = true;
        _logger.LogInformation("Application marked as ready");
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_isReady)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Application is ready to accept traffic"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Application is still initializing"));
    }
}
