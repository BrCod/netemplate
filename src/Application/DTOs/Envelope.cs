namespace Netemplate.Application.DTOs;

public sealed class Envelope<T>
{
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string? TenantId { get; init; }
    public int SchemaVersion { get; init; } = 1;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string EventType { get; init; } = typeof(T).Name;
    public T Payload { get; init; }

    public Envelope(T payload)
    {
        Payload = payload;
        CorrelationId = Guid.NewGuid();
    }
}
