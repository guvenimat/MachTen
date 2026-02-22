using FastEndpoints;
using MACHTEN.Api.Infrastructure.Caching;
using MACHTEN.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Api.Features.Products.GetProduct;

public sealed class GetProductEndpoint : Endpoint<GetProductRequest, GetProductResponse>
{
    private readonly MachtenCacheService _cache;
    private readonly MachtenDbContext _db;

    public GetProductEndpoint(MachtenCacheService cache, MachtenDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public override void Configure()
    {
        Get("/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a product by ID";
            s.Description = "Retrieves a product from cache (L1/L2) or falls back to database.";
        });
    }

    public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
    {
        // State-based factory avoids closure allocation over _db and req.Id
        var result = await _cache.GetOrCreateAsync(
            key: $"product:{req.Id}",
            state: (_db, req.Id),
            factory: static async (state, token) =>
            {
                var (db, productId) = state;
                var product = await db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productId, token);

                if (product is null)
                    return null!;

                return new GetProductResponse(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.CreatedAtUtc);
            },
            expiration: TimeSpan.FromMinutes(10),
            localExpiration: TimeSpan.FromMinutes(2),
            ct: ct);

        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
