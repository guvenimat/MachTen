using FastEndpoints;
using MACHTEN.Application.Features.Products.BulkCreateProducts;
using Wolverine;

namespace MACHTEN.Api.Features.Products.BulkCreateProducts;

public sealed class BulkCreateProductsEndpoint : Endpoint<BulkCreateProductsCommand, BulkCreateProductsResponse>
{
    private readonly IMessageBus _bus;

    public BulkCreateProductsEndpoint(IMessageBus bus) => _bus = bus;

    public override void Configure()
    {
        Post("/products/bulk");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Bulk create products";
            s.Description = "Inserts multiple products in a single batch. Returns count and elapsed time.";
        });
    }

    public override async Task HandleAsync(BulkCreateProductsCommand req, CancellationToken ct)
    {
        var result = await _bus.InvokeAsync<BulkCreateProductsResponse>(req, ct);
        await Send.OkAsync(result, ct);
    }
}
