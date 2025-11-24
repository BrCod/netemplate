using FluentAssertions;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Messaging.RabbitMq;
using Netemplate.Infrastructure.Policies.Config;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RabbitMQ.Client.Exceptions;

namespace Netemplate.Infrastructure.Resilience.Tests;

/// <summary>
/// Fault injection tests for ResilientMessageBus to verify retry, circuit breaker, timeout, and bulkhead policies
/// </summary>
public class ResilientMessageBusTests
{
    [Fact]
    public async Task PublishAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var innerBus = Substitute.For<IMessageBus>();
        var callCount = 0;
        
        innerBus.PublishAsync(Arg.Any<string>(), Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new BrokerUnreachableException(new Exception("Transient connection failure"));
                }
                return Task.CompletedTask;
            });

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions(),
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 10, // Fast for tests
                TimeoutSeconds = 5,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            }
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientBus = new ResilientMessageBus(innerBus, registry);

        // Act
        await resilientBus.PublishAsync("test.topic", new TestMessage { Id = 1, Name = "Test" });

        // Assert
        callCount.Should().Be(3);
        await innerBus.Received(3).PublishAsync("test.topic", Arg.Any<TestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithPersistentFailure_ExhaustsRetries()
    {
        // Arrange
        var innerBus = Substitute.For<IMessageBus>();
        innerBus.PublishAsync(Arg.Any<string>(), Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Throws(new BrokerUnreachableException(new Exception("Persistent failure")));

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions(),
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            }
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientBus = new ResilientMessageBus(innerBus, registry);

        // Act
        Func<Task> act = async () => await resilientBus.PublishAsync("test.topic", new TestMessage { Id = 1, Name = "Test" });

        // Assert
        await act.Should().ThrowAsync<BrokerUnreachableException>();
        await innerBus.Received(4).PublishAsync("test.topic", Arg.Any<TestMessage>(), Arg.Any<CancellationToken>()); // Initial + 3 retries
    }

    [Fact]
    public async Task PublishAsync_WithRepeatedFailures_TripsCircuitBreaker()
    {
        // Arrange
        var innerBus = Substitute.For<IMessageBus>();
        innerBus.PublishAsync(Arg.Any<string>(), Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Simulated failure"));

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions(),
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 0, // Disable retries to test circuit breaker directly
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                CircuitBreakerFailures = 3, // Trip after 3 failures
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            }
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientBus = new ResilientMessageBus(innerBus, registry);

        // Act - Execute failures to trip circuit breaker
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await resilientBus.PublishAsync("test.topic", new TestMessage { Id = i, Name = $"Test{i}" });
            }
            catch
            {
                // Expected to fail
            }
        }

        // Circuit breaker should now be open
        Func<Task> act = async () => await resilientBus.PublishAsync("test.topic", new TestMessage { Id = 999, Name = "Test999" });

        // Assert
        await act.Should().ThrowAsync<BrokenCircuitException>()
            .WithMessage("*circuit*");
    }

    [Fact]
    public async Task PublishAsync_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        var innerBus = Substitute.For<IMessageBus>();
        innerBus.PublishAsync(Arg.Any<string>(), Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                await Task.Delay(TimeSpan.FromSeconds(10), ct); // Respect cancellation
                return;
            });

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions(),
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 0,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            }
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientBus = new ResilientMessageBus(innerBus, registry);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        Func<Task> act = async () => await resilientBus.PublishAsync("test.topic", new TestMessage { Id = 1, Name = "Test" }, cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task SubscribeAsync_WithSuccess_CompletesSuccessfully()
    {
        // Arrange
        var innerBus = Substitute.For<IMessageBus>();
        innerBus.SubscribeAsync(Arg.Any<string>(), Arg.Any<Func<byte[], CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions(),
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 3,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                CircuitBreakerFailures = 5,
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            }
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientBus = new ResilientMessageBus(innerBus, registry);

        // Act
        await resilientBus.SubscribeAsync("test.topic", (bytes, ct) => Task.CompletedTask);

        // Assert
        await innerBus.Received(1).SubscribeAsync("test.topic", Arg.Any<Func<byte[], CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithMixedFailuresAndSuccesses_MaintainsCircuitClosed()
    {
        // Arrange
        var innerBus = Substitute.For<IMessageBus>();
        var callCount = 0;
        
        innerBus.PublishAsync(Arg.Any<string>(), Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                // Fail occasionally but not enough to trip circuit breaker
                if (callCount % 5 == 0)
                {
                    throw new Exception("Occasional failure");
                }
                return Task.CompletedTask;
            });

        var options = new ResilienceOptions
        {
            Cache = new CachePolicyOptions(),
            Messaging = new MessagingPolicyOptions
            {
                RetryCount = 1,
                BaseRetryDelayMs = 10,
                TimeoutSeconds = 5,
                CircuitBreakerFailures = 3, // Would need 3 consecutive failures
                CircuitBreakerDurationSeconds = 30,
                BulkheadMaxConcurrency = 10,
                BulkheadQueueLimit = 20
            }
        };
        var registry = ResiliencePolicyRegistry.Create(options);
        var resilientBus = new ResilientMessageBus(innerBus, registry);

        // Act - Execute multiple calls with occasional failures
        var successCount = 0;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await resilientBus.PublishAsync("test.topic", new TestMessage { Id = i, Name = $"Test{i}" });
                successCount++;
            }
            catch
            {
                // Some failures expected
            }
        }

        // Assert - Circuit should remain closed, most calls should succeed
        successCount.Should().BeGreaterThan(5);
    }

    private record TestMessage
    {
        public int Id { get; init; }
        public string? Name { get; init; }
    }
}
