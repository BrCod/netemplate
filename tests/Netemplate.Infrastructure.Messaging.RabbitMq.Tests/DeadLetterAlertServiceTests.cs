using FluentAssertions;
using Microsoft.Extensions.Logging;
using Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;
using NSubstitute;

namespace Netemplate.Infrastructure.Messaging.RabbitMq.Tests;

public class DeadLetterAlertServiceTests
{
    [Fact]
    public async Task AlertAsync_LogsErrorWithMessageDetails()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingDeadLetterAlertService>>();
        var service = new LoggingDeadLetterAlertService(logger);
        
        var message = new DeadLetterMessage
        {
            OriginalRoutingKey = "test.topic",
            OriginalExchange = "netemplate",
            Body = new byte[] { 1, 2, 3 },
            Reason = "rejected",
            RetryCount = 3,
            CorrelationId = "test-correlation-id",
            ExceptionMessage = "Test exception"
        };

        // Act
        await service.AlertAsync(message);

        // Assert
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("test.topic")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task AlertThresholdExceededAsync_LogsCriticalWithCounts()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingDeadLetterAlertService>>();
        var service = new LoggingDeadLetterAlertService(logger);

        // Act
        await service.AlertThresholdExceededAsync(150, 100);

        // Assert
        logger.Received(1).Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("150") && o.ToString()!.Contains("100")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
