using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Health
{
    /// <summary>
    /// Provides health check endpoints for the API.
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="healthCheckService">The health check service.</param>
        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Gets the liveness status of the API.
        /// </summary>
        /// <returns>OK if the API is running.</returns>
        [HttpGet("live")]
        public IActionResult Live() => Ok(new { status = "Healthy" });

        /// <summary>
        /// Gets the readiness status of the API and its dependencies.
        /// </summary>
        /// <returns>The health check results.</returns>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            var report = await _healthCheckService.CheckHealthAsync();
            return Ok(new
            {
                status = report.Status.ToString(),
                results = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    diagnostics = e.Value.Description
                })
            });
        }
    }
}
