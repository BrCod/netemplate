namespace Netemplate.Domain.ValueObjects;

public sealed class ProductName : ValueObject
{
    public string Value { get; }

    private ProductName(string value)
    {
        Value = value;
    }

    public static ProductName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Product name cannot be empty.", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 200)
            throw new ArgumentException("Product name must be between 3 and 200 characters.", nameof(value));

        if (trimmed.Any(char.IsControl))
            throw new ArgumentException("Product name cannot contain control characters.", nameof(value));

        return new ProductName(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
