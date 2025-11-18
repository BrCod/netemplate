using Netemplate.Domain.Events;
using Netemplate.Domain.ValueObjects;

namespace Netemplate.Domain.Entities;

public sealed class Product : BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public ProductName Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Product() { } // EF Core

    public static Product Create(ProductName name, Money price, string? description = null, Guid? correlationId = null)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        product._domainEvents.Add(new ProductCreatedEvent
        {
            ProductId = product.Id,
            Name = name.Value,
            Price = price.Amount,
            CorrelationId = correlationId ?? Guid.NewGuid()
        });

        return product;
    }

    public void Update(ProductName? name = null, Money? price = null, string? description = null, Guid? correlationId = null)
    {
        if (name != null) Name = name;
        if (price != null) Price = price;
        if (description != null) Description = description;

        UpdatedAt = DateTimeOffset.UtcNow;

        _domainEvents.Add(new ProductUpdatedEvent
        {
            ProductId = Id,
            CorrelationId = correlationId ?? Guid.NewGuid()
        });
    }

    public void Deactivate(Guid? correlationId = null)
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;

        _domainEvents.Add(new ProductDeactivatedEvent
        {
            ProductId = Id,
            CorrelationId = correlationId ?? Guid.NewGuid()
        });
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
