using FastEndpoints;
using MACHTEN.Api.Infrastructure.Logging;
using MACHTEN.Api.Persistence;
using MACHTEN.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Api.Features.Products.GetProducts;

public sealed class GetProductsEndpoint : EndpointWithoutRequest<List<GetProductsResponse>>
{
    private readonly MachtenDbContext _db;
    private readonly ILogger<GetProductsEndpoint> _logger;

    // Pre-compiled query: completely bypasses LINQ-to-SQL translation on every call.
    // The expression tree is compiled once and reused, eliminating repeated query
    // planning overhead. Combined with AsNoTracking + Select projection, this is
    // the fastest path EF Core can offer for read-heavy endpoints.
    private static readonly Func<MachtenDbContext, int, int, IAsyncEnumerable<GetProductsResponse>>
        CompiledGetProducts = EF.CompileAsyncQuery(
            (MachtenDbContext ctx, int skip, int take) =>
                ctx.Products
                    .AsNoTracking()
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .Skip(skip)
                    .Take(take)
                    .Select(p => new GetProductsResponse(
                        p.Id,
                        p.Name,
                        p.Price,
                        p.CreatedAtUtc)));

    public GetProductsEndpoint(MachtenDbContext db, ILogger<GetProductsEndpoint> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List products";
            s.Description = "Returns a paginated list of products using a pre-compiled EF Core query.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        var skip = (Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100);
        var take = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

        var results = new List<GetProductsResponse>(take);

        await foreach (var item in CompiledGetProducts(_db, skip, take).WithCancellation(ct))
        {
            results.Add(item);
        }

        _logger.LogProductsListed(results.Count);

        await Send.OkAsync(results, ct);
    }
}
