using Domain.Entities;

namespace Domain.Events
{
    /// <summary>
    /// Domain event emitted when a product is updated.
    /// </summary>
    public class ProductUpdatedEvent
    {
        /// <summary>The updated product.</summary>
        public Product Product { get; }
        /// <summary>UTC timestamp when the event occurred.</summary>
        public DateTime OccurredAt { get; }
        /// <summary>Initializes a new instance of ProductUpdatedEvent.</summary>
        public ProductUpdatedEvent(Product product)
        {
            Product = product;
            OccurredAt = DateTime.UtcNow;
        }
    }
}