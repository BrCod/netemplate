namespace Netemplate.Domain.Events;

public sealed class ProductCreatedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
