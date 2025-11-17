using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentValidation;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions and errors globally with RFC 7807 problem+json.
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = Activity.Current?.Id ?? context.TraceIdentifier;
            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

            _logger.LogError(exception, "Unhandled exception - CorrelationId: {CorrelationId}", correlationId);

            var response = context.Response;
            response.ContentType = "application/problem+json";

            var problemDetails = exception switch
            {
                ValidationException validationEx => new Application.DTOs.ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Error",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = "One or more validation errors occurred.",
                    Instance = context.Request.Path,
                    CorrelationId = correlationId,
                    TraceId = traceId,
                    Errors = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                },
                ArgumentNullException => new Application.DTOs.ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = "Invalid input parameter.",
                    Instance = context.Request.Path,
                    CorrelationId = correlationId,
                    TraceId = traceId
                },
                ArgumentException argEx => new Application.DTOs.ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request",
                    Status = (int)HttpStatusCode.BadRequest,
                    Detail = argEx.Message,
                    Instance = context.Request.Path,
                    CorrelationId = correlationId,
                    TraceId = traceId
                },
                UnauthorizedAccessException => new Application.DTOs.ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Status = (int)HttpStatusCode.Unauthorized,
                    Detail = "Access denied.",
                    Instance = context.Request.Path,
                    CorrelationId = correlationId,
                    TraceId = traceId
                },
                _ => new Application.DTOs.ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Internal Server Error",
                    Status = (int)HttpStatusCode.InternalServerError,
                    Detail = "An error occurred while processing your request.",
                    Instance = context.Request.Path,
                    CorrelationId = correlationId,
                    TraceId = traceId
                }
            };

            response.StatusCode = problemDetails.Status;
            await response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}