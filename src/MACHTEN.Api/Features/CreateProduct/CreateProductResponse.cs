namespace MACHTEN.Api.Features.CreateProduct;

public sealed class CreateProductResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
}
