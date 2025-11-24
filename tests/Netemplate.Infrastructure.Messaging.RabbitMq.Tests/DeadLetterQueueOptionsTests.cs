using FluentAssertions;
using Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

namespace Netemplate.Infrastructure.Messaging.RabbitMq.Tests;

public class DeadLetterQueueOptionsTests
{
    [Fact]
    public void DeadLetterQueueOptions_HasDefaultValues()
    {
        // Arrange & Act
        var options = new DeadLetterQueueOptions();

        // Assert
        options.DeadLetterExchange.Should().Be("netemplate.dlx");
        options.DeadLetterQueue.Should().Be("netemplate.dlq");
        options.AlertThreshold.Should().Be(100);
        options.MessageTtlSeconds.Should().Be(86400);
        options.EnableMonitoring.Should().BeTrue();
        options.MaxRetries.Should().Be(3);
    }

    [Fact]
    public void DeadLetterQueueOptions_CanBeCustomized()
    {
        // Arrange & Act
        var options = new DeadLetterQueueOptions
        {
            DeadLetterExchange = "custom.dlx",
            DeadLetterQueue = "custom.dlq",
            AlertThreshold = 200,
            MessageTtlSeconds = 3600,
            EnableMonitoring = false,
            MaxRetries = 5
        };

        // Assert
        options.DeadLetterExchange.Should().Be("custom.dlx");
        options.DeadLetterQueue.Should().Be("custom.dlq");
        options.AlertThreshold.Should().Be(200);
        options.MessageTtlSeconds.Should().Be(3600);
        options.EnableMonitoring.Should().BeFalse();
        options.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void GetDeadLetterQueueArguments_ReturnsCorrectArguments()
    {
        // Arrange
        var deadLetterExchange = "test.dlx";
        var maxRetries = 5;

        // Act
        var args = DeadLetterQueueHandler.GetDeadLetterQueueArguments(deadLetterExchange, maxRetries);

        // Assert
        args.Should().ContainKey("x-dead-letter-exchange");
        args["x-dead-letter-exchange"].Should().Be(deadLetterExchange);
        args.Should().ContainKey("x-dead-letter-routing-key");
        args["x-dead-letter-routing-key"].Should().Be("dlq");
        args.Should().ContainKey("x-max-length");
        args["x-max-length"].Should().Be(maxRetries);
    }
}
