using Domain.Entities;

namespace Application.Validators
{
    /// <summary>
    /// Validator for Product entity creation.
    /// </summary>
    public class ProductCreateValidator
    {
        /// <summary>Validate Product creation rules.</summary>
        public bool Validate(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name) || product.Name.Length < 3 || product.Name.Length > 200)
                return false;
            if (product.Price < 0)
                return false;
            if (product.Description != null && product.Description.Length > 1000)
                return false;
            return true;
        }
    }
}
