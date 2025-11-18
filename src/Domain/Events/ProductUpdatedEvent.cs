namespace Netemplate.Domain.Events;

public sealed class ProductUpdatedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
}
