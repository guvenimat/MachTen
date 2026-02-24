using MACHTEN.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Application.Features.Products.GetProducts;

public static class GetProductsHandler
{
    public static async Task<List<GetProductsResponse>> HandleAsync(
        GetProductsQuery query,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var skip = (Math.Max(query.Page, 1) - 1) * Math.Clamp(query.PageSize, 1, 100);
        var take = Math.Clamp(query.PageSize, 1, 100);

        return await db.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(p => new GetProductsResponse(
                p.Id,
                p.Name,
                p.Price,
                p.CreatedAtUtc))
            .ToListAsync(ct);
    }
}
