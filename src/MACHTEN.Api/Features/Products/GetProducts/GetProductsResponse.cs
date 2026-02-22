namespace MACHTEN.Api.Features.Products.GetProducts;

public sealed record GetProductsResponse(
    Guid Id,
    string Name,
    decimal Price,
    DateTime CreatedAtUtc);
