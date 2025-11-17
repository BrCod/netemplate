namespace Application.DTOs
{
    /// <summary>
    /// RFC 7807 Problem Details response model.
    /// </summary>
    public class ProblemDetails
    {
        /// <summary>URI reference identifying the problem type.</summary>
        public string Type { get; set; } = "about:blank";
        
        /// <summary>Short, human-readable summary.</summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>HTTP status code.</summary>
        public int Status { get; set; }
        
        /// <summary>Human-readable explanation specific to this occurrence.</summary>
        public string? Detail { get; set; }
        
        /// <summary>URI reference identifying the specific occurrence.</summary>
        public string? Instance { get; set; }
        
        /// <summary>Correlation ID for distributed tracing.</summary>
        public string? CorrelationId { get; set; }
        
        /// <summary>Trace ID from distributed tracing context.</summary>
        public string? TraceId { get; set; }
        
        /// <summary>Validation errors keyed by field name.</summary>
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
