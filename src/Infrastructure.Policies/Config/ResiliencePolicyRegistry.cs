using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.CircuitBreaker;
using Polly.Bulkhead;

namespace Netemplate.Infrastructure.Policies.Config;

public interface IResiliencePolicyRegistry
{
    CachePolicies Cache { get; }
    MessagingPolicies Messaging { get; }
}

public sealed class ResiliencePolicyRegistry : IResiliencePolicyRegistry
{
    public CachePolicies Cache { get; }
    public MessagingPolicies Messaging { get; }

    private ResiliencePolicyRegistry(CachePolicies cache, MessagingPolicies messaging)
    {
        Cache = cache;
        Messaging = messaging;
    }

    public static ResiliencePolicyRegistry Create(ResilienceOptions options)
    {
        var cacheRetry = Policy.Handle<Exception>()
            .WaitAndRetryAsync(options.Cache.RetryCount, attempt => TimeSpan.FromMilliseconds(options.Cache.BaseRetryDelayMs * attempt));
        var cacheTimeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(options.Cache.TimeoutSeconds));
        var cacheBulkhead = Policy.BulkheadAsync(options.Cache.BulkheadMaxConcurrency, options.Cache.BulkheadQueueLimit);

        var messagingRetry = Policy.Handle<Exception>()
            .WaitAndRetryAsync(options.Messaging.RetryCount, attempt => TimeSpan.FromMilliseconds(options.Messaging.BaseRetryDelayMs * attempt));
        var messagingTimeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(options.Messaging.TimeoutSeconds));
        var messagingCircuit = Policy.Handle<Exception>()
            .CircuitBreakerAsync(options.Messaging.CircuitBreakerFailures, TimeSpan.FromSeconds(options.Messaging.CircuitBreakerDurationSeconds));
        var messagingBulkhead = Policy.BulkheadAsync(options.Messaging.BulkheadMaxConcurrency, options.Messaging.BulkheadQueueLimit);

        return new ResiliencePolicyRegistry(
            new CachePolicies(cacheRetry, cacheTimeout, cacheBulkhead),
            new MessagingPolicies(messagingRetry, messagingTimeout, messagingCircuit, messagingBulkhead));
    }
}

public sealed record CachePolicies(AsyncRetryPolicy Retry, AsyncTimeoutPolicy Timeout, AsyncBulkheadPolicy Bulkhead);
public sealed record MessagingPolicies(AsyncRetryPolicy Retry, AsyncTimeoutPolicy Timeout, AsyncCircuitBreakerPolicy CircuitBreaker, AsyncBulkheadPolicy Bulkhead);
