namespace Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for Product operations.
    /// </summary>
    public class ProductDto
    {
        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the product description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the product price.
        /// </summary>
        public decimal Price { get; set; }
    }
}
