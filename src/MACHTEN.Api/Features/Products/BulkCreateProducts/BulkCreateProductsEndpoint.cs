using FastEndpoints;
using MACHTEN.Api.Infrastructure.Logging;
using Wolverine;

namespace MACHTEN.Api.Features.Products.BulkCreateProducts;

public sealed class BulkCreateProductsEndpoint : Endpoint<BulkCreateProductsCommand, BulkCreateProductsResponse>
{
    private readonly IMessageBus _bus;
    private readonly ILogger<BulkCreateProductsEndpoint> _logger;

    public BulkCreateProductsEndpoint(IMessageBus bus, ILogger<BulkCreateProductsEndpoint> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/products/bulk");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Bulk create products";
            s.Description = "Inserts multiple products in a single EF Core batch. Returns count and elapsed time for benchmarking.";
        });
    }

    public override async Task HandleAsync(BulkCreateProductsCommand req, CancellationToken ct)
    {
        var result = await _bus.InvokeAsync<BulkCreateProductsResponse>(req, ct);

        _logger.LogProductsListed(result.InsertedCount);

        await Send.OkAsync(result, ct);
    }
}
