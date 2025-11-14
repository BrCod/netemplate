using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Api.Middleware.RateLimiting
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            // Implement global and per-endpoint rate limiting, return 429 with Retry-After
            await _next(context);
        }
    }
}
