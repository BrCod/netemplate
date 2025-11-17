using System;
using Domain.Events;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a product entity in the domain with lifecycle management.
    /// </summary>
    public class Product
    {
        private readonly List<object> _domainEvents = new();

        /// <summary>Product unique identifier.</summary>
        public Guid Id { get; set; }
        /// <summary>Product name (3-200 chars, unique per tenant).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Optional product description (max 1000 chars).</summary>
        public string? Description { get; set; }
        /// <summary>Product price (non-negative, scale 2).</summary>
        public decimal Price { get; set; }
        /// <summary>UTC creation timestamp.</summary>
        public DateTime CreatedAt { get; private set; }
        /// <summary>UTC last update timestamp.</summary>
        public DateTime UpdatedAt { get; private set; }
        /// <summary>Product active status.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Domain events pending dispatch.</summary>
        public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Creates a new product with default active state.
        /// </summary>
        public static Product Create(Guid id, string name, string? description, decimal price)
        {
            var product = new Product
            {
                Id = id,
                Name = name,
                Description = description,
                Price = price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            product.AddDomainEvent(new ProductCreatedEvent(product));
            return product;
        }

        /// <summary>
        /// Updates product properties and emits update event.
        /// </summary>
        public void Update(string name, string? description, decimal price)
        {
            Name = name;
            Description = description;
            Price = price;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new ProductUpdatedEvent(this));
        }

        /// <summary>
        /// Soft deactivates the product (marks as inactive without physical deletion).
        /// </summary>
        /// <param name="reason">Optional reason for deactivation.</param>
        public void Deactivate(string? reason = null)
        {
            if (!IsActive)
                throw new InvalidOperationException("Product is already deactivated.");
            
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new ProductDeactivatedEvent(this, reason));
        }

        /// <summary>
        /// Reactivates a previously deactivated product.
        /// </summary>
        public void Reactivate()
        {
            if (IsActive)
                throw new InvalidOperationException("Product is already active.");
            
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a domain event to the pending events list.
        /// </summary>
        private void AddDomainEvent(object domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Clears all pending domain events (called after dispatch).
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
