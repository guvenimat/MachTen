using FastEndpoints;
using MACHTEN.Application.Features.Products.GetProducts;
using Wolverine;

namespace MACHTEN.Api.Features.Products.GetProducts;

public sealed class GetProductsEndpoint : EndpointWithoutRequest<List<GetProductsResponse>>
{
    private readonly IMessageBus _bus;

    public GetProductsEndpoint(IMessageBus bus) => _bus = bus;

    public override void Configure()
    {
        Get("/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List products";
            s.Description = "Returns a paginated list of products.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        var query = new GetProductsQuery(
            Page: page == 0 ? 1 : page,
            PageSize: pageSize == 0 ? 20 : pageSize);

        var result = await _bus.InvokeAsync<List<GetProductsResponse>>(query, ct);

        await Send.OkAsync(result, ct);
    }
}
