using System;

namespace Application.DTOs
{
    /// <summary>
    /// Envelope for event/message metadata.
    /// </summary>
    public class MessageEnvelope
    {
        /// <summary>Correlation ID for distributed tracing.</summary>
        public Guid CorrelationId { get; set; }
        /// <summary>Causation ID for event lineage.</summary>
        public Guid? CausationId { get; set; }
        /// <summary>Tenant identifier (multi-tenancy).</summary>
        public string? TenantId { get; set; }
        /// <summary>Schema version for envelope evolution.</summary>
        public int SchemaVersion { get; set; }
        /// <summary>UTC timestamp of event creation.</summary>
        public DateTime Timestamp { get; set; }
        /// <summary>Type of event/message.</summary>
        public string EventType { get; set; } = string.Empty;
        /// <summary>Serialized payload object.</summary>
        public object Payload { get; set; } = default!;
    }
}
