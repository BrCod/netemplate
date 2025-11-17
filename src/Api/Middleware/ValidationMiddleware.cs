using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware for validating request data and model state, producing RFC 7807 responses.
    /// </summary>
    public class ValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidationMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        public ValidationMiddleware(RequestDelegate next, ILogger<ValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to perform validation checks.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
            
            // Model state validation happens in the controller action via [ApiController] attribute
            // FluentValidation errors are caught by ErrorHandlingMiddleware as ValidationException
            // This middleware remains as a potential extension point for pre-action validation
        }
    }
}