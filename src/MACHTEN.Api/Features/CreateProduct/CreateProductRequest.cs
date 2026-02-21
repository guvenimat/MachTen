namespace MACHTEN.Api.Features.CreateProduct;

public sealed class CreateProductRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
}
