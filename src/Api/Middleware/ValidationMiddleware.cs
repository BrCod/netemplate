using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware for validating request data and model state.
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
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation failed: {ValidationError}", ex.Message);
                await HandleValidationExceptionAsync(context, ex);
            }
        }

        private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = (int)HttpStatusCode.BadRequest;

            var result = new
            {
                error = "Validation Error",
                message = exception.Message,
                statusCode = (int)HttpStatusCode.BadRequest
            };

            await response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}