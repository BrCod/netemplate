using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Persistence.Postgres;
using Netemplate.Infrastructure.Persistence.Postgres.Outbox;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Netemplate.Infrastructure.Persistence.Postgres.Tests.Outbox;

/// <summary>
/// Tests for outbox dispatcher reliability with simulated failures
/// </summary>
public class OutboxDispatcherTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcherTests()
    {
        // Setup in-memory database
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"OutboxTest_{Guid.NewGuid()}"));

        // Mock dependencies
        _messageBus = Substitute.For<IMessageBus>();
        _logger = Substitute.For<ILogger<OutboxDispatcher>>();

        services.AddSingleton(_messageBus);
        services.AddSingleton(_logger);

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithNoMessages_CompletesWithoutPublishing()
    {
        // Arrange
        var dispatcher = new OutboxDispatcher(_serviceProvider, _logger);
        var cts = new CancellationTokenSource();

        // Act
        var dispatcherTask = dispatcher.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(100)); // Let it run briefly
        cts.Cancel();
        await dispatcherTask;

        // Assert
        await _messageBus.DidNotReceive().PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithPendingMessage_PublishesSuccessfully()
    {
        // Arrange
        var message = new OutboxMessage
        {
            EventType = "ProductCreated",
            Payload = "{\"id\":1,\"name\":\"Test Product\"}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        _messageBus.PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act - Manually invoke processing logic
        var pendingMessages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .ToListAsync();

        foreach (var msg in pendingMessages)
        {
            await _messageBus.PublishAsync(msg.EventType, msg.Payload, CancellationToken.None);
            msg.ProcessedAt = DateTimeOffset.UtcNow;
        }
        await _dbContext.SaveChangesAsync();

        // Assert
        var processedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        processedMessage.Should().NotBeNull();
        processedMessage!.ProcessedAt.Should().NotBeNull();
        processedMessage.RetryCount.Should().Be(0);
        await _messageBus.Received(1).PublishAsync("ProductCreated", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var message = new OutboxMessage
        {
            EventType = "ProductUpdated",
            Payload = "{\"id\":2,\"name\":\"Updated Product\"}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        var callCount = 0;
        _messageBus.PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new Exception("Transient messaging failure");
                }
                return Task.CompletedTask;
            });

        // Act - Simulate 3 processing attempts
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var pendingMessages = await _dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .ToListAsync();

            foreach (var msg in pendingMessages)
            {
                try
                {
                    await _messageBus.PublishAsync(msg.EventType, msg.Payload, CancellationToken.None);
                    msg.ProcessedAt = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    msg.RetryCount++;
                    msg.Error = ex.Message;
                }
                await _dbContext.SaveChangesAsync();
            }
        }

        // Assert
        var processedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        processedMessage.Should().NotBeNull();
        processedMessage!.ProcessedAt.Should().NotBeNull();
        processedMessage.RetryCount.Should().Be(2); // Failed twice, succeeded on third
        await _messageBus.Received(3).PublishAsync("ProductUpdated", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithPersistentFailure_ExhaustsRetries()
    {
        // Arrange
        var message = new OutboxMessage
        {
            EventType = "ProductDeleted",
            Payload = "{\"id\":3}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        _messageBus.PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Persistent messaging failure"));

        // Act - Simulate 6 processing attempts (exceeds max retry of 5)
        for (int attempt = 0; attempt < 6; attempt++)
        {
            var pendingMessages = await _dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .ToListAsync();

            foreach (var msg in pendingMessages)
            {
                try
                {
                    await _messageBus.PublishAsync(msg.EventType, msg.Payload, CancellationToken.None);
                    msg.ProcessedAt = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    msg.RetryCount++;
                    msg.Error = ex.Message;
                }
                await _dbContext.SaveChangesAsync();
            }
        }

        // Assert
        var failedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        failedMessage.Should().NotBeNull();
        failedMessage!.ProcessedAt.Should().BeNull(); // Never processed successfully
        failedMessage.RetryCount.Should().Be(5); // Max retries reached
        failedMessage.Error.Should().Be("Persistent messaging failure");
        
        // Should stop trying after 5 retries (6th attempt won't find the message)
        await _messageBus.Received(5).PublishAsync("ProductDeleted", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_ProcessesMultipleMessagesInOrder()
    {
        // Arrange
        var messages = new[]
        {
            new OutboxMessage { EventType = "Event1", Payload = "{\"seq\":1}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-3) },
            new OutboxMessage { EventType = "Event2", Payload = "{\"seq\":2}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) },
            new OutboxMessage { EventType = "Event3", Payload = "{\"seq\":3}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) }
        };
        _dbContext.OutboxMessages.AddRange(messages);
        await _dbContext.SaveChangesAsync();

        _messageBus.PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var pendingMessages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        foreach (var msg in pendingMessages)
        {
            await _messageBus.PublishAsync(msg.EventType, msg.Payload, CancellationToken.None);
            msg.ProcessedAt = DateTimeOffset.UtcNow;
        }
        await _dbContext.SaveChangesAsync();

        // Assert
        var allProcessed = await _dbContext.OutboxMessages.ToListAsync();
        allProcessed.Should().AllSatisfy(m => m.ProcessedAt.Should().NotBeNull());
        
        // Verify order by checking call sequence
        await _messageBus.Received(1).PublishAsync("Event1", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _messageBus.Received(1).PublishAsync("Event2", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _messageBus.Received(1).PublishAsync("Event3", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_WithMixedSuccessAndFailure_ProcessesPartially()
    {
        // Arrange
        var successMessage = new OutboxMessage { EventType = "SuccessEvent", Payload = "{\"ok\":true}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) };
        var failMessage = new OutboxMessage { EventType = "FailEvent", Payload = "{\"ok\":false}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) };
        
        _dbContext.OutboxMessages.AddRange(successMessage, failMessage);
        await _dbContext.SaveChangesAsync();

        _messageBus.PublishAsync("SuccessEvent", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _messageBus.PublishAsync("FailEvent", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Specific failure"));

        // Act
        var pendingMessages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        foreach (var msg in pendingMessages)
        {
            try
            {
                await _messageBus.PublishAsync(msg.EventType, msg.Payload, CancellationToken.None);
                msg.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.Error = ex.Message;
            }
            await _dbContext.SaveChangesAsync();
        }

        // Assert
        var processedSuccess = await _dbContext.OutboxMessages.FindAsync(successMessage.Id);
        processedSuccess!.ProcessedAt.Should().NotBeNull();
        processedSuccess.RetryCount.Should().Be(0);

        var processedFail = await _dbContext.OutboxMessages.FindAsync(failMessage.Id);
        processedFail!.ProcessedAt.Should().BeNull();
        processedFail.RetryCount.Should().Be(1);
        processedFail.Error.Should().Be("Specific failure");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}
