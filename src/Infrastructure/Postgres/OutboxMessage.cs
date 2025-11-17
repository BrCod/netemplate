using System;
using System.Text.Json;

namespace Infrastructure.Postgres
{
    /// <summary>
    /// Represents an outbox message for reliable event publishing.
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the event type name.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON serialized event payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the message was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the message was processed.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of retry attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the last error message if processing failed.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Deserializes the payload to the specified event type.
        /// </summary>
        /// <typeparam name="T">The event type to deserialize to.</typeparam>
        /// <returns>The deserialized event.</returns>
        public T? GetEvent<T>() where T : class
        {
            return JsonSerializer.Deserialize<T>(Payload);
        }

        /// <summary>
        /// Creates a new outbox message from an event.
        /// </summary>
        /// <param name="event">The event to store.</param>
        /// <returns>A new outbox message.</returns>
        public static OutboxMessage Create(object @event)
        {
            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = @event.GetType().Name,
                Payload = JsonSerializer.Serialize(@event),
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };
        }
    }
}