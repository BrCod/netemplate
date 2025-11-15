using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Api.Observability
{
    /// <summary>
    /// Configuration for OpenTelemetry observability.
    /// </summary>
    public static class OpenTelemetryConfig
    {
        /// <summary>
        /// Configures OpenTelemetry tracing and metrics for the application.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public static void Configure(IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("NetemplateApi"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("NetemplateApi")
                )
                .WithMetrics(builder => builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                );
        }
    }
}
