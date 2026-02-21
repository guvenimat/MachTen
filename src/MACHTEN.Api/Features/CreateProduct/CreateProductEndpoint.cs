using FastEndpoints;

namespace MACHTEN.Api.Features.CreateProduct;

public sealed class CreateProductEndpoint : Endpoint<CreateProductRequest, CreateProductResponse>
{
    public override void Configure()
    {
        Post("/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new product";
            s.Description = "Creates a product and returns the created resource.";
        });
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        // TODO: replace with actual persistence via Application layer
        var response = new CreateProductResponse
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Price = req.Price
        };

        await Send.CreatedAtAsync<CreateProductEndpoint>(
            routeValues: new { response.Id },
            responseBody: response,
            cancellation: ct);
    }
}
