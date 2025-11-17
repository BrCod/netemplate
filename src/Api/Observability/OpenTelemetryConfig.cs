using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;

namespace Api.Observability
{
    /// <summary>
    /// Configuration for OpenTelemetry observability.
    /// </summary>
    public static class OpenTelemetryConfig
    {
        /// <summary>
        /// Activity source for custom tracing.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new("NetemplateApi");

        /// <summary>
        /// Configures OpenTelemetry tracing and metrics for the application.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public static void Configure(IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("NetemplateApi")
                        .AddAttributes(new[]
                        {
                            new KeyValuePair<string, object>("service.version", "1.0.0"),
                            new KeyValuePair<string, object>("service.environment", "development")
                        }))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            // Add correlation ID to traces
                            if (request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
                            {
                                activity.SetTag("correlation.id", correlationId.ToString());
                            }
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.status_code", response.StatusCode);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.client.method", request.Method?.Method);
                            activity.SetTag("http.client.uri", request.RequestUri?.ToString());
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("http.client.status_code", (int)response.StatusCode);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.command.text", command.CommandText);
                            activity.SetTag("db.command.type", command.CommandType.ToString());
                        };
                    })
                    .AddSource("NetemplateApi")
                    .AddSource("Infrastructure.Postgres.OutboxDispatcher")
                )
                .WithMetrics(builder => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("NetemplateApi"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("NetemplateApi")
                );
        }
    }
}
