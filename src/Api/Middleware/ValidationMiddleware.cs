using Microsoft.AspNetCore.Mvc;

namespace Netemplate.Api.Middleware;

public sealed class ValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.HasJsonContentType() && context.Request.ContentLength > 0)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status415UnsupportedMediaType,
                Title = "Unsupported Media Type",
                Detail = "Only application/json content type is supported.",
                Instance = context.Request.Path
            };

            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problemDetails);
            return;
        }

        await _next(context);
    }
}
