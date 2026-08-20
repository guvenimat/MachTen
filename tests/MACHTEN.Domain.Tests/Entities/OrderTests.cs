using MACHTEN.Domain.Entities;
using MACHTEN.Domain.Events;
using MACHTEN.Domain.Exceptions;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Domain.Tests.Entities;

public class OrderTests
{
    [Fact]
    public void Place_RaisesOrderPlaced()
    {
        var order = Order.Place("acme-42", new Money(19.99m, "TRY"));

        var placed = Assert.IsType<OrderPlaced>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, placed.OrderId);
        Assert.Equal(19.99m, placed.Amount);
        Assert.Equal("TRY", placed.Currency);
    }

    [Fact]
    public void Place_TrimsCustomerReferenceAndStartsAsPlaced()
    {
        var order = Order.Place("  acme-42  ", new Money(5m, "USD"));

        Assert.Equal("acme-42", order.CustomerReference);
        Assert.Equal(OrderStatus.Placed, order.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_RejectsMissingCustomerReference(string reference)
    {
        Assert.Throws<InvalidOrderException>(() => Order.Place(reference, new Money(5m, "USD")));
    }

    [Fact]
    public void Place_RejectsZeroTotal()
    {
        Assert.Throws<InvalidOrderException>(() => Order.Place("acme-42", Money.Zero("USD")));
    }

    [Fact]
    public void ClearDomainEvents_EmptiesThePendingList()
    {
        var order = Order.Place("acme-42", new Money(5m, "USD"));

        order.ClearDomainEvents();

        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void Place_GeneratesTimeOrderedIds()
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

        Assert.Equal(7, (bytes[6] & 0xF0) >> 4);

        long milliseconds = 0;
        for (var i = 0; i < 6; i++)
        {
            milliseconds = (milliseconds << 8) | bytes[i];
        }

        var stamped = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        Assert.InRange(stamped, before, DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
