namespace MACHTEN.Domain.ValueObjects;

/// <summary>
/// Zero-allocation DDD value object. As a readonly record struct, it lives entirely
/// on the stack -- no heap allocation, no GC pressure. Equality and GetHashCode
/// are compiler-generated from the positional parameters.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static readonly Money Zero = new(0m, "USD");

    public Money Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot add {Currency} to {other.Currency}");

        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");

        return this with { Amount = Amount - other.Amount };
    }

    public Money Multiply(decimal factor) => this with { Amount = Amount * factor };

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);

    public override string ToString() => $"{Amount:F2} {Currency}";
}
