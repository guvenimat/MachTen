using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Domain.Entities;

namespace MACHTEN.Application.Features.Products.CreateProduct;

public static class CreateProductHandler
{
    public static async Task<CreateProductResponse> HandleAsync(
        CreateProductCommand command,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var product = Product.Create(command.Name, command.Description, command.Price);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return new CreateProductResponse(product.Id);
    }
}
