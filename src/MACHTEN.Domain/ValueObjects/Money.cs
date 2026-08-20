using System.Globalization;

namespace MACHTEN.Domain.ValueObjects;

public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Display form, e.g. "19.99 TRY". Invariant culture on purpose: this value
    /// goes out over the wire, so it must not shift with the server's locale.
    /// </summary>
    public string Formatted => string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");

    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");

        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => Formatted;
}
