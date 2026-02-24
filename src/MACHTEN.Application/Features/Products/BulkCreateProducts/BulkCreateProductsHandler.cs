using System.Diagnostics;
using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Domain.Entities;

namespace MACHTEN.Application.Features.Products.BulkCreateProducts;

public static class BulkCreateProductsHandler
{
    public static async Task<BulkCreateProductsResponse> HandleAsync(
        BulkCreateProductsCommand command,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var products = new Product[command.Products.Count];
        for (var i = 0; i < command.Products.Count; i++)
        {
            var dto = command.Products[i];
            products[i] = Product.Create(dto.Name, dto.Description, dto.Price);
        }

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        sw.Stop();

        return new BulkCreateProductsResponse(products.Length, sw.ElapsedMilliseconds);
    }
}
