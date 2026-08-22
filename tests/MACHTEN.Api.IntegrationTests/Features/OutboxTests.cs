using MACHTEN.Application.Features.Orders.PlaceOrder;
using MACHTEN.Domain.Events;
using Wolverine.Tracking;

namespace MACHTEN.Api.IntegrationTests.Features;

/// <summary>
/// Proves the domain event actually leaves the handler.
///
/// This existed as configuration long before it worked: Order.Place recorded
/// OrderPlaced, the handler never published it, and the broker it was routed to
/// had never started — so nothing surfaced. Recording an event is not
/// publishing one, and only a test that waits for delivery can tell them apart.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class OutboxTests(MachtenApiFactory factory)
{
    [Fact]
    public async Task PlacingAnOrder_PublishesOrderPlaced()
    {
        var command = new PlaceOrderCommand("outbox-probe", 42.50m, "try");

        // Waits for the whole cascade to settle: the command handled, and every
        // message it produced delivered to its own handler.
        var session = await factory.Services
            .TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .InvokeMessageAndWaitAsync(command);

        var published = session.Executed.SingleMessage<OrderPlaced>();

        Assert.Equal("outbox-probe", published.CustomerReference);
        Assert.Equal(42.50m, published.Amount);
        Assert.Equal("TRY", published.Currency);
    }
}
