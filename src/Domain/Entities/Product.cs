using System;


namespace Domain.Entities
{
    /// <summary>
    /// Represents a product entity in the domain.
    /// </summary>
    public class Product
    {
        /// <summary>Product unique identifier.</summary>
        public Guid Id { get; set; }
        /// <summary>Product name (3-200 chars, unique per tenant).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Optional product description (max 1000 chars).</summary>
        public string? Description { get; set; }
        /// <summary>Product price (non-negative, scale 2).</summary>
        public decimal Price { get; set; }
        /// <summary>UTC creation timestamp.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>UTC last update timestamp.</summary>
        public DateTime UpdatedAt { get; set; }
        /// <summary>Product active status.</summary>
        public bool IsActive { get; set; }
    }
}
