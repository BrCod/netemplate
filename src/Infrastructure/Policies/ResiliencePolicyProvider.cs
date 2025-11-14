using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Infrastructure.Policies
{
    public static class ResiliencePolicyProvider
    {
        public static IAsyncPolicy GetDefaultPolicy()
        {
            return Policy.WrapAsync(
                Policy.TimeoutAsync(3),
                Policy.Handle<Exception>().RetryAsync(5),
                Policy.Handle<Exception>().CircuitBreakerAsync(5, TimeSpan.FromSeconds(30))
            );
        }
    }
}
