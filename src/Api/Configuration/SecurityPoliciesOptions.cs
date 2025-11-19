namespace Netemplate.Api.Configuration;

public sealed class SecurityPoliciesOptions
{
    public const string SectionName = "SecurityPolicies";

    public RateLimitingOptions RateLimiting { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public RequestLimitsOptions RequestLimits { get; set; } = new();
}

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    public int QueueLimit { get; set; } = 10;
}

public sealed class CorsOptions
{
    public bool Enabled { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public bool AllowCredentials { get; set; } = true;
}

public sealed class RequestLimitsOptions
{
    public long MaxRequestBodySizeBytes { get; set; } = 5_242_880; // 5MB
    public int MaxConcurrentRequests { get; set; } = 1000;
}
