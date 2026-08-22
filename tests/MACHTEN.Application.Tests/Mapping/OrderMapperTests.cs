using MACHTEN.Application.Mapping;
using MACHTEN.Domain.Entities;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Application.Tests.Mapping;

public class OrderMapperTests
{
    [Test]
    public async Task ToDto_FlattensTheMoneyValueObject()
    {
        var order = Order.Place("acme-42", new Money(19.99m, "try"));

        var dto = OrderMapper.ToDto(order);

        await Assert.That(dto.Id).IsEqualTo(order.Id);
        await Assert.That(dto.CustomerReference).IsEqualTo("acme-42");
        await Assert.That(dto.Amount).IsEqualTo(19.99m);
        await Assert.That(dto.Currency).IsEqualTo("TRY");
        await Assert.That(dto.Formatted).IsEqualTo("19.99 TRY");
        await Assert.That(dto.Status).IsEqualTo("Placed");
    }
}
