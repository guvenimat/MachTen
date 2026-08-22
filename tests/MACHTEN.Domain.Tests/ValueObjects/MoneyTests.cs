using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Test]
    public async Task Constructor_NormalizesCurrencyToUppercase()
    {
        var money = new Money(10, "usd");

        await Assert.That(money.Currency).IsEqualTo("USD");
    }

    [Test]
    [Arguments(-1)]
    [Arguments(-0.01)]
    public async Task Constructor_ThrowsForNegativeAmount(decimal amount)
    {
        await Assert.That(() => new Money(amount, "USD")).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments("")]
    [Arguments("US")]
    [Arguments("USDD")]
    public async Task Constructor_ThrowsForInvalidCurrencyCode(string currency)
    {
        await Assert.That(() => new Money(10, currency)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Add_SumsAmounts_WhenCurrenciesMatch()
    {
        var result = new Money(10, "USD").Add(new Money(5, "USD"));

        await Assert.That(result.Amount).IsEqualTo(15);
        await Assert.That(result.Currency).IsEqualTo("USD");
    }

    [Test]
    public async Task Add_Throws_WhenCurrenciesDiffer()
    {
        var usd = new Money(10, "USD");
        var eur = new Money(5, "EUR");

        await Assert.That(() => usd.Add(eur)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Zero_CreatesMoneyWithZeroAmount()
    {
        var money = Money.Zero("TRY");

        await Assert.That(money.Amount).IsEqualTo(0);
        await Assert.That(money.Currency).IsEqualTo("TRY");
    }
}
