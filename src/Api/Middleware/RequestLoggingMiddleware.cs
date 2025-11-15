using System.Diagnostics;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to log request and response information.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Starting request {Method} {Path} - CorrelationId: {CorrelationId}",
                context.Request.Method, context.Request.Path, correlationId);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Completed request {Method} {Path} in {ElapsedMs}ms with status {StatusCode} - CorrelationId: {CorrelationId}",
                    context.Request.Method, 
                    context.Request.Path, 
                    stopwatch.ElapsedMilliseconds, 
                    context.Response.StatusCode,
                    correlationId);
            }
        }
    }
}