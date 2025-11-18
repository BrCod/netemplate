namespace Netemplate.Domain.Events;

public sealed class ProductDeactivatedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
}
