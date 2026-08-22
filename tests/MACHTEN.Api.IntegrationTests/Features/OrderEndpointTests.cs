using System.Net;
using System.Net.Http.Json;
using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Application.Features.Orders.PlaceOrder;
using MACHTEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MACHTEN.Api.IntegrationTests.Features;

public class OrderEndpointTests
{
    [ClassDataSource<MachtenApiFactory>(Shared = SharedType.PerTestSession)]
    public required MachtenApiFactory Factory { get; init; }

    /// <summary>
    /// The "writes" limiter partitions on X-Client-Id, so giving each test its
    /// own id keeps the burst test from spending the budget the others need.
    /// </summary>
    private HttpClient CreateClient(string clientId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Id", clientId);
        return client;
    }

    [Test]
    public async Task PlaceOrder_ThenFetch_RoundTripsThroughTheDomain()
    {
        var client = CreateClient(nameof(PlaceOrder_ThenFetch_RoundTripsThroughTheDomain));

        var placed = await client.PostAsJsonAsync("/api/v1/orders",
            new PlaceOrderCommand("acme-42", 19.99m, "try"));
        var created = await placed.Content.ReadFromJsonAsync<PlaceOrderResponse>();

        await Assert.That(placed.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(created!.Total).IsEqualTo("19.99 TRY");

        var fetched = await client.GetAsync($"/api/v1/orders/{created.OrderId}");
        var order = await fetched.Content.ReadFromJsonAsync<OrderDto>();

        await Assert.That(fetched.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(order!.CustomerReference).IsEqualTo("acme-42");
        await Assert.That(order.Currency).IsEqualTo("TRY");          // domain uppercased it
        await Assert.That(order.Status).IsEqualTo("Placed");
    }

    [Test]
    public async Task GetOrder_ReturnsNotFound_ForUnknownId()
    {
        var client = CreateClient(nameof(GetOrder_ReturnsNotFound_ForUnknownId));

        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Proves reads are actually served from HybridCache rather than the
    /// database: the row is changed behind the cache's back, and the endpoint
    /// keeps returning the cached value. If caching silently stopped working,
    /// the second read would pick up the new amount and this test would fail.
    /// </summary>
    [Test]
    public async Task GetOrder_IsServedFromCache_NotTheDatabase()
    {
        var client = CreateClient(nameof(GetOrder_IsServedFromCache_NotTheDatabase));

        var placed = await client.PostAsJsonAsync("/api/v1/orders",
            new PlaceOrderCommand("cache-probe", 10m, "USD"));
        var created = await placed.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        var url = $"/api/v1/orders/{created!.OrderId}";

        // Populates the cache.
        var first = await client.GetFromJsonAsync<OrderDto>(url);
        await Assert.That(first!.Amount).IsEqualTo(10m);

        // Mutate the row directly, bypassing the application entirely.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MachtenDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Orders SET TotalAmount = 999 WHERE Id = {0}", created.OrderId);
        }

        var second = await client.GetFromJsonAsync<OrderDto>(url);

        await Assert.That(second!.Amount).IsEqualTo(10m);
    }

    /// <summary>
    /// The "writes" limiter permits 100 requests per minute with a queue of 10,
    /// so a burst past that must start returning 429.
    /// </summary>
    [Test]
    public async Task PlaceOrder_IsRateLimited_UnderABurst()
    {
        var client = CreateClient(nameof(PlaceOrder_IsRateLimited_UnderABurst));

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 140; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/orders",
                new PlaceOrderCommand($"burst-{i}", 1m, "USD"));
            statuses.Add(response.StatusCode);
        }

        await Assert.That(statuses).Contains(HttpStatusCode.TooManyRequests);
    }
}
