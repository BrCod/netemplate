namespace Netemplate.Api.Middleware;

public sealed class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly long _maxRequestBodySize;

    public RequestSizeLimitMiddleware(RequestDelegate next, long maxRequestBodySize = 5_242_880) // 5MB default
    {
        _next = next;
        _maxRequestBodySize = maxRequestBodySize;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxRequestBodySize)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.11",
                title = "Payload Too Large",
                status = 413,
                detail = $"Request body exceeds maximum allowed size of {_maxRequestBodySize} bytes.",
                traceId = context.TraceIdentifier
            });
            return;
        }

        var maxRequestBodySizeFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (maxRequestBodySizeFeature != null && !maxRequestBodySizeFeature.IsReadOnly)
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = _maxRequestBodySize;
        }

        await _next(context);
    }
}
