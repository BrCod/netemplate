using System.Net;
using System.Text.Json;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions and errors globally.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to handle errors and exceptions.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var result = exception switch
            {
                ArgumentNullException => new { 
                    error = "Bad Request", 
                    message = "Invalid input parameter", 
                    statusCode = (int)HttpStatusCode.BadRequest 
                },
                ArgumentException => new { 
                    error = "Bad Request", 
                    message = exception.Message, 
                    statusCode = (int)HttpStatusCode.BadRequest 
                },
                UnauthorizedAccessException => new { 
                    error = "Unauthorized", 
                    message = "Access denied", 
                    statusCode = (int)HttpStatusCode.Unauthorized 
                },
                _ => new { 
                    error = "Internal Server Error", 
                    message = "An error occurred while processing your request", 
                    statusCode = (int)HttpStatusCode.InternalServerError 
                }
            };

            response.StatusCode = result.statusCode;
            await response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}