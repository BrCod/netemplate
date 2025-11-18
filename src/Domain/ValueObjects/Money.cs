namespace Netemplate.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        var supportedCurrencies = new[] { "USD", "EUR", "GBP", "CAD" };
        if (!supportedCurrencies.Contains(currency))
            throw new ArgumentException($"Currency {currency} is not supported.", nameof(currency));

        return new Money(amount, currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
