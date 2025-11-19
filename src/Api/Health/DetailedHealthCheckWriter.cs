using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Netemplate.Api.Health;

public sealed class DetailedHealthCheckWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
                exception = entry.Value.Exception?.Message,
                tags = entry.Value.Tags
            }).ToArray()
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
