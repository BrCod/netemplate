using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Infrastructure.Policies
{
    /// <summary>
    /// Provides resilience policies using Polly for handling transient faults.
    /// Implements retry, circuit breaker, and timeout patterns for database, cache, and messaging operations.
    /// </summary>
    public class ResiliencePolicyProvider
    {
        /// <summary>
        /// Gets a combined policy for repository operations (timeout → retry → circuit breaker).
        /// </summary>
        /// <typeparam name="TResult">The return type of the operation.</typeparam>
        /// <returns>Wrapped async policy with resilience for database operations.</returns>
        public IAsyncPolicy<TResult> GetRepositoryPolicy<TResult>()
        {
            // Timeout: 5 seconds
            var timeoutPolicy = Policy.TimeoutAsync<TResult>(TimeSpan.FromSeconds(5));

            // Retry: 3 attempts with exponential backoff
            var retryPolicy = Policy
                .Handle<Exception>()
                .OrResult<TResult>(r => false) // Always retry on exception, never on result
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100)
                );

            // Circuit breaker: opens after 5 failures for 30 seconds
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .OrResult<TResult>(r => false)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Wrap in order: timeout (inner) → retry → circuit breaker (outer)
            return Policy.WrapAsync<TResult>(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
        }

        /// <summary>
        /// Gets a combined policy for cache operations (timeout → retry, no circuit breaker).
        /// </summary>
        /// <typeparam name="TResult">The return type of the operation.</typeparam>
        /// <returns>Wrapped async policy optimized for cache operations.</returns>
        public IAsyncPolicy<TResult> GetCachePolicy<TResult>()
        {
            // Timeout: 5 seconds
            var timeoutPolicy = Policy.TimeoutAsync<TResult>(TimeSpan.FromSeconds(5));

            // Retry: 2 attempts with shorter backoff
            var retryPolicy = Policy
                .Handle<Exception>()
                .OrResult<TResult>(r => false)
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(50 * attempt)
                );

            // Wrap in order: timeout (inner) → retry (outer)
            return Policy.WrapAsync<TResult>(timeoutPolicy, retryPolicy);
        }

        /// <summary>
        /// Gets a combined policy for message publisher operations (timeout → retry → circuit breaker).
        /// </summary>
        /// <typeparam name="TResult">The return type of the operation.</typeparam>
        /// <returns>Wrapped async policy for reliable message publishing.</returns>
        public IAsyncPolicy<TResult> GetPublisherPolicy<TResult>()
        {
            // Timeout: 5 seconds
            var timeoutPolicy = Policy.TimeoutAsync<TResult>(TimeSpan.FromSeconds(5));

            // Retry: 3 attempts with exponential backoff
            var retryPolicy = Policy
                .Handle<Exception>()
                .OrResult<TResult>(r => false)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100)
                );

            // Circuit breaker: opens after 5 failures for 30 seconds
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .OrResult<TResult>(r => false)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Wrap in order: timeout (inner) → retry → circuit breaker (outer)
            return Policy.WrapAsync<TResult>(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
        }
    }
}
