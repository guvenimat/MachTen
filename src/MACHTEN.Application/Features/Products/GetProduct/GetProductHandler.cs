using MACHTEN.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Application.Features.Products.GetProduct;

public static class GetProductHandler
{
    public static async Task<GetProductResponse?> HandleAsync(
        GetProductQuery query,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.Id, ct);

        if (product is null)
            return null;

        return new GetProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.CreatedAtUtc);
    }
}
