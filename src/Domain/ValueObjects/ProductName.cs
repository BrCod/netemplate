namespace Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a validated product name.
    /// </summary>
    public class ProductName
    {
        /// <summary>The validated product name value.</summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of ProductName with validation.
        /// </summary>
        /// <param name="value">The product name string.</param>
        public ProductName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Product name cannot be empty or whitespace.", nameof(value));
            if (value.Length < 3 || value.Length > 200)
                throw new ArgumentException("Product name must be between 3 and 200 characters.", nameof(value));
            
            Value = value.Trim();
        }

        /// <summary>Implicit conversion to string.</summary>
        public static implicit operator string(ProductName productName) => productName.Value;
        /// <summary>Explicit conversion from string.</summary>
        public static explicit operator ProductName(string value) => new ProductName(value);

        /// <summary>Returns the string representation.</summary>
        public override string ToString() => Value;
        /// <summary>Checks equality with another object.</summary>
        public override bool Equals(object? obj) => obj is ProductName other && Value == other.Value;
        /// <summary>Gets the hash code.</summary>
        public override int GetHashCode() => Value.GetHashCode();
    }
}