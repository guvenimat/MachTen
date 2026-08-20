using FastEndpoints;
using MACHTEN.Application.Features.Orders.PlaceOrder;
using Wolverine;

namespace MACHTEN.Api.Features.Orders;

public sealed class PlaceOrderEndpoint(IMessageBus bus)
    : Endpoint<PlaceOrderCommand, PlaceOrderResponse>
{
    public override void Configure()
    {
        Post("/orders");
        Version(1);
        AllowAnonymous();

        // Writes are the expensive path, so this is where the limiter earns its
        // keep. Configured in Program.cs as the "writes" policy.
        Options(x => x.RequireRateLimiting("writes"));

        Summary(s =>
        {
            s.Summary = "Place an order";
            s.Description = "Persists the order and publishes OrderPlaced through the transactional outbox.";
        });
    }

    public override async Task HandleAsync(PlaceOrderCommand req, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PlaceOrderResponse>(req, ct);
        await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
    }
}
