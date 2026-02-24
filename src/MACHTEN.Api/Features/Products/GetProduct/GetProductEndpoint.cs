using FastEndpoints;
using MACHTEN.Application.Features.Products.GetProduct;
using Wolverine;

namespace MACHTEN.Api.Features.Products.GetProduct;

public sealed class GetProductEndpoint : Endpoint<GetProductRequest, GetProductResponse>
{
    private readonly IMessageBus _bus;

    public GetProductEndpoint(IMessageBus bus) => _bus = bus;

    public override void Configure()
    {
        Get("/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a product by ID";
            s.Description = "Retrieves a product by its unique identifier.";
        });
    }

    public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
    {
        var result = await _bus.InvokeAsync<GetProductResponse?>(new GetProductQuery(req.Id), ct);

        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}

public sealed class GetProductRequest
{
    public required Guid Id { get; init; }
}
