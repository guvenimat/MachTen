namespace MACHTEN.Application.Features.Products.GetProduct;

public sealed record GetProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAtUtc);
