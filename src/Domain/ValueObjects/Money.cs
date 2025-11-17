namespace Domain.ValueObjects
{
    /// <summary>
    /// Value object representing monetary amount with currency.
    /// </summary>
    public class Money
    {
        /// <summary>The monetary amount.</summary>
        public decimal Amount { get; }
        /// <summary>The currency code.</summary>
        public string Currency { get; }

        /// <summary>
        /// Initializes a new instance of Money.
        /// </summary>
        /// <param name="amount">The monetary amount.</param>
        /// <param name="currency">The currency code (defaults to USD).</param>
        public Money(decimal amount, string currency = "USD")
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative.", nameof(amount));
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency code is required.", nameof(currency));
            
            Amount = amount;
            Currency = currency.ToUpperInvariant();
        }

        /// <summary>Adds two Money instances of the same currency.</summary>
        public static Money operator +(Money a, Money b)
        {
            if (a.Currency != b.Currency)
                throw new InvalidOperationException($"Cannot add {a.Currency} and {b.Currency}.");
            return new Money(a.Amount + b.Amount, a.Currency);
        }

        /// <summary>Returns the string representation.</summary>
        public override string ToString() => $"{Amount:F2} {Currency}";
        /// <summary>Checks equality with another object.</summary>
        public override bool Equals(object? obj) => obj is Money other && Amount == other.Amount && Currency == other.Currency;
        /// <summary>Gets the hash code.</summary>
        public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    }
}