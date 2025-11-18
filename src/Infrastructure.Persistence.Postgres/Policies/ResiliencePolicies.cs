using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Netemplate.Infrastructure.Policies;

public static class ResiliencePolicies
{
    public static AsyncRetryPolicy CreateRetryPolicy(int retryCount = 3)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retry, ctx) =>
                {
                    Console.WriteLine($"Retry {retry} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                });
    }

    public static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(
        int exceptionsBeforeBreaking = 5,
        int durationOfBreakSeconds = 30)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: exceptionsBeforeBreaking,
                durationOfBreak: TimeSpan.FromSeconds(durationOfBreakSeconds),
                onBreak: (exception, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {exception.Message}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }

    public static AsyncTimeoutPolicy CreateTimeoutPolicy(int timeoutSeconds = 30)
    {
        return Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds));
    }
}
