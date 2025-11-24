using FluentAssertions;
using Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

namespace Netemplate.Infrastructure.Messaging.RabbitMq.Tests;

public class DeadLetterMessageTests
{
    [Fact]
    public void DeadLetterMessage_CanBeCreated()
    {
        // Arrange
        var body = new byte[] { 1, 2, 3, 4, 5 };
        var headers = new Dictionary<string, object?> { ["test-header"] = "test-value" };
        
        // Act
        var message = new DeadLetterMessage
        {
            OriginalRoutingKey = "products.created",
            OriginalExchange = "netemplate",
            Body = body,
            Reason = "rejected",
            RetryCount = 3,
            ExceptionMessage = "Test exception",
            ExceptionStackTrace = "at Test.Method()",
            CorrelationId = "abc-123",
            Headers = headers
        };

        // Assert
        message.OriginalRoutingKey.Should().Be("products.created");
        message.OriginalExchange.Should().Be("netemplate");
        message.Body.Should().Equal(body);
        message.Reason.Should().Be("rejected");
        message.RetryCount.Should().Be(3);
        message.ExceptionMessage.Should().Be("Test exception");
        message.ExceptionStackTrace.Should().Be("at Test.Method()");
        message.CorrelationId.Should().Be("abc-123");
        message.Headers.Should().ContainKey("test-header");
        message.DeadLetteredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DeadLetterMessage_DeadLetteredAt_DefaultsToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;
        
        // Act
        var message = new DeadLetterMessage
        {
            OriginalRoutingKey = "test",
            OriginalExchange = "test",
            Body = Array.Empty<byte>(),
            Reason = "test"
        };
        
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        message.DeadLetteredAt.Should().BeOnOrAfter(beforeCreation);
        message.DeadLetteredAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void DeadLetterMessage_OptionalPropertiesCanBeNull()
    {
        // Arrange & Act
        var message = new DeadLetterMessage
        {
            OriginalRoutingKey = "test",
            OriginalExchange = "test",
            Body = Array.Empty<byte>(),
            Reason = "test",
            ExceptionMessage = null,
            ExceptionStackTrace = null,
            CorrelationId = null,
            Headers = null
        };

        // Assert
        message.ExceptionMessage.Should().BeNull();
        message.ExceptionStackTrace.Should().BeNull();
        message.CorrelationId.Should().BeNull();
        message.Headers.Should().BeNull();
    }
}
