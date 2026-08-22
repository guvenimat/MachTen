using MACHTEN.Domain.Entities;
using MACHTEN.Domain.Events;
using MACHTEN.Domain.Exceptions;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Domain.Tests.Entities;

public class OrderTests
{
    [Test]
    public async Task Place_RaisesOrderPlaced()
    {
        var order = Order.Place("acme-42", new Money(19.99m, "TRY"));

        await Assert.That(order.DomainEvents).HasSingleItem();

        var placed = await Assert.That(order.DomainEvents.Single()).IsTypeOf<OrderPlaced>();

        await Assert.That(placed!.OrderId).IsEqualTo(order.Id);
        await Assert.That(placed.Amount).IsEqualTo(19.99m);
        await Assert.That(placed.Currency).IsEqualTo("TRY");
    }

    [Test]
    public async Task Place_TrimsCustomerReferenceAndStartsAsPlaced()
    {
        var order = Order.Place("  acme-42  ", new Money(5m, "USD"));

        await Assert.That(order.CustomerReference).IsEqualTo("acme-42");
        await Assert.That(order.Status).IsEqualTo(OrderStatus.Placed);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Place_RejectsMissingCustomerReference(string reference)
    {
        await Assert.That(() => Order.Place(reference, new Money(5m, "USD")))
            .Throws<InvalidOrderException>();
    }

    [Test]
    public async Task Place_RejectsZeroTotal()
    {
        await Assert.That(() => Order.Place("acme-42", Money.Zero("USD")))
            .Throws<InvalidOrderException>();
    }

    [Test]
    public async Task ClearDomainEvents_EmptiesThePendingList()
    {
        var order = Order.Place("acme-42", new Money(5m, "USD"));

        order.ClearDomainEvents();

        await Assert.That(order.DomainEvents).IsEmpty();
    }

    [Test]
    public async Task Place_GeneratesTimeOrderedIds()
    {
        // Version 7 GUIDs put a 48-bit millisecond timestamp in the leading
        // bytes, so inserts land at the end of a clustered index instead of
        // scattering across pages.
        //
        // Asserted on the timestamp itself rather than by comparing two ids:
        // ordering is only guaranteed across different milliseconds, and two
        // orders placed back to back usually share one — that version of this
        // test failed intermittently for exactly that reason.
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var bytes = Order.Place("a", new Money(1m, "USD")).Id.ToByteArray(bigEndian: true);

        await Assert.That((bytes[6] & 0xF0) >> 4).IsEqualTo(7);

        long milliseconds = 0;
        for (var i = 0; i < 6; i++)
        {
            milliseconds = (milliseconds << 8) | bytes[i];
        }

        var stamped = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

        await Assert.That(stamped).IsBetween(before, DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
