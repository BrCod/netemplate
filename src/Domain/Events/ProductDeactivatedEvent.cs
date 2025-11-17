using Domain.Entities;

namespace Domain.Events
{
    /// <summary>
    /// Domain event emitted when a product is deactivated (soft delete).
    /// </summary>
    public class ProductDeactivatedEvent
    {
        /// <summary>The deactivated product.</summary>
        public Product Product { get; }
        /// <summary>UTC timestamp when the event occurred.</summary>
        public DateTime OccurredAt { get; }
        /// <summary>Reason for deactivation.</summary>
        public string? Reason { get; }
        /// <summary>Initializes a new instance of ProductDeactivatedEvent.</summary>
        public ProductDeactivatedEvent(Product product, string? reason = null)
        {
            Product = product;
            OccurredAt = DateTime.UtcNow;
            Reason = reason;
        }
    }
}