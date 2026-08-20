using MACHTEN.Application.Features.Money;

namespace MACHTEN.Application.Tests.Features.Money;

public class FormatMoneyHandlerTests
{
    [Fact]
    public void Handle_MapsDomainValuesOntoDto()
    {
        var result = FormatMoneyHandler.Handle(new FormatMoneyCommand(19.99m, "try"));

        Assert.Equal(19.99m, result.Amount);
        Assert.Equal("TRY", result.Currency);
        Assert.Equal("19.99 TRY", result.Formatted);
    }

    [Fact]
    public void Handle_PropagatesDomainRule_ForNegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FormatMoneyHandler.Handle(new FormatMoneyCommand(-1, "USD")));
    }
}
