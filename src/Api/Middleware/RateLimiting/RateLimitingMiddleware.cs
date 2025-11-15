using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Api.Middleware.RateLimiting
{
    /// <summary>
    /// Middleware for applying rate limiting to API requests.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        /// <summary>
        /// Invokes the middleware to apply rate limiting.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Implement global and per-endpoint rate limiting, return 429 with Retry-After
            await _next(context);
        }
    }
}
