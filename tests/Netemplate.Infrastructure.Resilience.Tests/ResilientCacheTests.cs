using FluentAssertions;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Cache.Redis;
using Netemplate.Infrastructure.Policies.Config;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly.Timeout;
using StackExchange.Redis;

namespace Netemplate.Infrastructure.Resilience.Tests;

/// <summary>
/// Fault injection tests for ResilientCache to verify retry, timeout, and bulkhead policies
/// </summary>
public class ResilientCacheTests
{
    [Fact]
    public async Task GetAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var innerCache = Substitute.For<ICache>();
        var callCount = 0;
        
        innerCache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Transient failure");
                }
                return Task.FromResult<string?>("cached-value");
            });

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 10, // Fast for tests
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            },
            Messaging = new MessagingPolicyOptions()
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientCache = new ResilientCache(innerCache, registry);

        // Act
        var result = await resilientCache.GetAsync<string>("test-key");

        // Assert
        result.Should().Be("cached-value");
        callCount.Should().Be(3); // Failed twice, succeeded on third attempt
        await innerCache.Received(3).GetAsync<string>("test-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithPersistentFailure_ExhaustsRetries()
    {
        // Arrange
        var innerCache = Substitute.For<ICache>();
        innerCache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Persistent failure"));

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            },
            Messaging = new MessagingPolicyOptions()
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientCache = new ResilientCache(innerCache, registry);

        // Act
        Func<Task> act = async () => await resilientCache.GetAsync<string>("test-key");

        // Assert
        await act.Should().ThrowAsync<RedisConnectionException>();
        await innerCache.Received(4).GetAsync<string>("test-key", Arg.Any<CancellationToken>()); // Initial + 3 retries
    }

    [Fact]
    public async Task SetAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var innerCache = Substitute.For<ICache>();
        var callCount = 0;
        
        innerCache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw new RedisTimeoutException("Transient timeout", CommandStatus.Unknown);
                }
                return Task.CompletedTask;
            });

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            },
            Messaging = new MessagingPolicyOptions()
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientCache = new ResilientCache(innerCache, registry);

        // Act
        await resilientCache.SetAsync("test-key", "test-value", TimeSpan.FromMinutes(5));

        // Assert
        callCount.Should().Be(2);
        await innerCache.Received(2).SetAsync("test-key", "test-value", TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        var innerCache = Substitute.For<ICache>();
        innerCache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                await Task.Delay(TimeSpan.FromSeconds(10), ct); // Respect cancellation
                return (string?)"delayed-value";
            });

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 0,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            },
            Messaging = new MessagingPolicyOptions()
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientCache = new ResilientCache(innerCache, registry);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        Func<Task> act = async () => await resilientCache.GetAsync<string>("test-key", cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task RemoveAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var innerCache = Substitute.For<ICache>();
        var callCount = 0;
        
        innerCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new RedisServerException("Transient server error");
                }
                return Task.CompletedTask;
            });

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            },
            Messaging = new MessagingPolicyOptions()
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientCache = new ResilientCache(innerCache, registry);

        // Act
        await resilientCache.RemoveAsync("test-key");

        // Assert
        callCount.Should().Be(2);
        await innerCache.Received(2).RemoveAsync("test-key", Arg.Any<CancellationToken>());
    }
}
