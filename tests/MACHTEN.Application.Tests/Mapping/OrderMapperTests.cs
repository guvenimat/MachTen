using MACHTEN.Application.Mapping;
using MACHTEN.Domain.Entities;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Application.Tests.Mapping;

public class OrderMapperTests
{
    [Fact]
    public void ToDto_FlattensTheMoneyValueObject()
    {
        var order = Order.Place("acme-42", new Money(19.99m, "try"));

        var dto = OrderMapper.ToDto(order);

        Assert.Equal(order.Id, dto.Id);
        Assert.Equal("acme-42", dto.CustomerReference);
        Assert.Equal(19.99m, dto.Amount);
        Assert.Equal("TRY", dto.Currency);
        Assert.Equal("19.99 TRY", dto.Formatted);
        Assert.Equal("Placed", dto.Status);
    }
}
