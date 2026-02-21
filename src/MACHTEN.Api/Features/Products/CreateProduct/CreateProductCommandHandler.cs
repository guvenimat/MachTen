using MACHTEN.Api.Persistence;
using MACHTEN.Domain.Entities;

namespace MACHTEN.Api.Features.Products.CreateProduct;

public static class CreateProductCommandHandler
{
    public static async Task<CreateProductResponse> HandleAsync(
        CreateProductCommand command,
        MachtenDbContext db,
        CancellationToken ct)
    {
        var product = Product.Create(command.Name, command.Description, command.Price);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return new CreateProductResponse(product.Id);
    }
}
