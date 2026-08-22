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
public class OutboxTests
{
    [ClassDataSource<MachtenApiFactory>(Shared = SharedType.PerTestSession)]
    public required MachtenApiFactory Factory { get; init; }

    [Test]
    public async Task PlacingAnOrder_PublishesOrderPlaced()
    {
        // Unique per run: tests share one host and run in parallel, so the
        // tracking session sees every message in flight, not just this one's.
        var reference = $"outbox-probe-{Guid.CreateVersion7():N}";
        var command = new PlaceOrderCommand(reference, 42.50m, "try");

        // Waits for the whole cascade to settle: the command handled, and every
        // message it produced delivered to its own handler.
        var session = await Factory.Services
            .TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .InvokeMessageAndWaitAsync(command);

        var published = session.Executed
            .MessagesOf<OrderPlaced>()
            .Single(m => m.CustomerReference == reference);

        await Assert.That(published.CustomerReference).IsEqualTo(reference);
        await Assert.That(published.Amount).IsEqualTo(42.50m);
        await Assert.That(published.Currency).IsEqualTo("TRY");
    }
}
