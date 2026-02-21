using FastEndpoints;
using Wolverine;

namespace MACHTEN.Api.Features.Products.CreateProduct;

public sealed class CreateProductEndpoint : Endpoint<CreateProductCommand, CreateProductResponse>
{
    private readonly IMessageBus _bus;

    public CreateProductEndpoint(IMessageBus bus) => _bus = bus;

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

    public override async Task HandleAsync(CreateProductCommand req, CancellationToken ct)
    {
        var result = await _bus.InvokeAsync<CreateProductResponse>(req, ct);
        await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
    }
}
