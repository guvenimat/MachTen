using FastEndpoints;
using MACHTEN.Application.Contracts;
using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Application.Features.Orders.GetOrder;

namespace MACHTEN.Api.Features.Orders;

public sealed class GetOrderEndpoint(IApplicationDbContext db, ICacheStore cache)
    : EndpointWithoutRequest<OrderDto>
{
    public override void Configure()
    {
        Get("/orders/{id:guid}");
        Version(1);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Fetch an order by id";
            s.Description = "Served through HybridCache: in-process L1, then Garnet L2, then the database.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var order = await GetOrderHandler.HandleAsync(new GetOrderQuery(id), db, cache, ct);

        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(order, ct);
    }
}
