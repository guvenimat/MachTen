using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_NormalizesCurrencyToUppercase()
    {
        var money = new Money(10, "usd");

        Assert.Equal("USD", money.Currency);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Constructor_ThrowsForNegativeAmount(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, "USD"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Constructor_ThrowsForInvalidCurrencyCode(string currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(10, currency));
    }

    [Fact]
    public void Add_SumsAmounts_WhenCurrenciesMatch()
    {
        var result = new Money(10, "USD").Add(new Money(5, "USD"));

        Assert.Equal(15, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Add_Throws_WhenCurrenciesDiffer()
    {
        var usd = new Money(10, "USD");
        var eur = new Money(5, "EUR");

        Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
    }

    [Fact]
    public void Zero_CreatesMoneyWithZeroAmount()
    {
        var money = Money.Zero("TRY");

        Assert.Equal(0, money.Amount);
        Assert.Equal("TRY", money.Currency);
    }
}
