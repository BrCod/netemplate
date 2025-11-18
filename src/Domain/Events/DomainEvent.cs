namespace Netemplate.Domain.Events;

public abstract class DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    // Envelope metadata kept in-domain to avoid referencing Application layer
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string? TenantId { get; init; }
    public int SchemaVersion { get; init; } = 1;
}
