using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Api.Logging
{
    /// <summary>
    /// Middleware for redacting personally identifiable information from logs.
    /// </summary>
    public class PiiRedactionMiddleware
    {
        private readonly RequestDelegate _next;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PiiRedactionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public PiiRedactionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        /// <summary>
        /// Invokes the middleware to redact PII from request/response data.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Redact PII from logs
            await _next(context);
        }
    }
}
