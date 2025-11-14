using System;
using Domain.Entities;


namespace Domain.Events
{
    /// <summary>
    /// Domain event emitted when a product is created.
    /// </summary>
    public class ProductCreatedEvent
    {
        /// <summary>The created product.</summary>
        public Product Product { get; }
        /// <summary>UTC timestamp when the event occurred.</summary>
        public DateTime OccurredAt { get; }
        /// <summary>Initializes a new instance of ProductCreatedEvent.</summary>
        public ProductCreatedEvent(Product product)
        {
            Product = product;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
