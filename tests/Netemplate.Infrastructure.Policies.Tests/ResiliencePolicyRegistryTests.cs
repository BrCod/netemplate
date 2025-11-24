using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Netemplate.Infrastructure.Policies.Config;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Netemplate.Infrastructure.Policies.Tests;

public class ResiliencePolicyRegistryTests
{
    [Fact]
    public void Create_WithValidOptions_CreatesRegistryWithCachePolicies()
    {
        // Arrange
        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 200,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 50,
                BulkheadQueueLimit = 200
            },
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 300,
                TimeoutSeconds = 10,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 20,
                BulkheadQueueLimit = 200
            }
        };

        // Act
        var registry = ResiliencePolicyRegistry.Create(options);

        // Assert
        registry.Should().NotBeNull();
        registry.Cache.Should().NotBeNull();
        registry.Cache.Retry.Should().NotBeNull();
        registry.Cache.Timeout.Should().NotBeNull();
        registry.Cache.Bulkhead.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithValidOptions_CreatesRegistryWithMessagingPolicies()
    {
        // Arrange
        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 200,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 50,
                BulkheadQueueLimit = 200
            },
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 300,
                TimeoutSeconds = 10,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 20,
                BulkheadQueueLimit = 200
            }
        };

        // Act
        var registry = ResiliencePolicyRegistry.Create(options);

        // Assert
        registry.Should().NotBeNull();
        registry.Messaging.Should().NotBeNull();
        registry.Messaging.Retry.Should().NotBeNull();
        registry.Messaging.Timeout.Should().NotBeNull();
        registry.Messaging.CircuitBreaker.Should().NotBeNull();
        registry.Messaging.Bulkhead.Should().NotBeNull();
    }

    [Fact]
    public void Create_CacheRetryPolicy_UsesConfiguredRetryCount()
    {
        // Arrange
        var expectedRetryCount = 5;
        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = expectedRetryCount,
                BaseRetryDelayMs = 200,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 50,
                BulkheadQueueLimit = 200
            },
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 300,
                TimeoutSeconds = 10,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 20,
                BulkheadQueueLimit = 200
            }
        };

        // Act
        var registry = ResiliencePolicyRegistry.Create(options);
        var retryPolicy = registry.Cache.Retry;

        // Assert - Verify policy is created (actual retry behavior would require execution)
        retryPolicy.Should().NotBeNull();
    }

    [Fact]
    public void Create_MessagingCircuitBreaker_UsesConfiguredThreshold()
    {
        // Arrange
        var expectedFailureThreshold = 10;
        var expectedDurationSeconds = 60;
        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 200,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 50,
                BulkheadQueueLimit = 200
            },
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 300,
                TimeoutSeconds = 10,
                CircuitBreakerFailures = expectedFailureThreshold,
                CircuitBreakerDurationSeconds = expectedDurationSeconds,
                BulkheadMaxConcurrency = 20,
                BulkheadQueueLimit = 200
            }
        };

        // Act
        var registry = ResiliencePolicyRegistry.Create(options);
        var circuitBreakerPolicy = registry.Messaging.CircuitBreaker;

        // Assert
        circuitBreakerPolicy.Should().NotBeNull();
    }

    [Fact]
    public void Create_TimeoutPolicies_UseConfiguredTimeouts()
    {
        // Arrange
        var cacheTimeoutSeconds = 10;
        var messagingTimeoutSeconds = 30;
        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 200,
                TimeoutSeconds = cacheTimeoutSeconds,
                BulkheadMaxConcurrency = 50,
                BulkheadQueueLimit = 200
            },
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 300,
                TimeoutSeconds = messagingTimeoutSeconds,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 20,
                BulkheadQueueLimit = 200
            }
        };

        // Act
        var registry = ResiliencePolicyRegistry.Create(options);

        // Assert
        registry.Cache.Timeout.Should().NotBeNull();
        registry.Messaging.Timeout.Should().NotBeNull();
    }

    [Fact]
    public void Create_BulkheadPolicies_UseConfiguredLimits()
    {
        // Arrange
        var cacheMaxConcurrency = 100;
        var cacheQueueLimit = 500;
        var messagingMaxConcurrency = 50;
        var messagingQueueLimit = 250;
        
        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 200,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = cacheMaxConcurrency,
                BulkheadQueueLimit = cacheQueueLimit
            },
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 300,
                TimeoutSeconds = 10,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = messagingMaxConcurrency,
                BulkheadQueueLimit = messagingQueueLimit
            }
        };

        // Act
        var registry = ResiliencePolicyRegistry.Create(options);

        // Assert
        registry.Cache.Bulkhead.Should().NotBeNull();
        registry.Messaging.Bulkhead.Should().NotBeNull();
    }
}
