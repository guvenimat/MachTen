using System.Net;
using System.Net.Http.Json;
using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Application.Features.Orders.PlaceOrder;
using MACHTEN.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MACHTEN.Api.IntegrationTests.Features;

public class OrderEndpointTests(MachtenApiFactory factory) : IClassFixture<MachtenApiFactory>
{
    [Fact]
    public async Task PlaceOrder_ThenFetch_RoundTripsThroughTheDomain()
    {
        var client = factory.CreateClient();

        var placed = await client.PostAsJsonAsync("/api/v1/orders",
            new PlaceOrderCommand("acme-42", 19.99m, "try"));
        var created = await placed.Content.ReadFromJsonAsync<PlaceOrderResponse>();

        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
        Assert.Equal("19.99 TRY", created!.Total);

        var fetched = await client.GetAsync($"/api/v1/orders/{created.OrderId}");
        var order = await fetched.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal("acme-42", order!.CustomerReference);
        Assert.Equal("TRY", order.Currency);          // domain uppercased it
        Assert.Equal("Placed", order.Status);
    }

    [Fact]
    public async Task GetOrder_ReturnsNotFound_ForUnknownId()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Proves reads are actually served from HybridCache rather than the
    /// database: the row is changed behind the cache's back, and the endpoint
    /// keeps returning the cached value. If caching silently stopped working,
    /// the second read would pick up the new amount and this test would fail.
    /// </summary>
    [Fact]
    public async Task GetOrder_IsServedFromCache_NotTheDatabase()
    {
        var client = factory.CreateClient();

        var placed = await client.PostAsJsonAsync("/api/v1/orders",
            new PlaceOrderCommand("cache-probe", 10m, "USD"));
        var created = await placed.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        var url = $"/api/v1/orders/{created!.OrderId}";

        // Populates the cache.
        var first = await client.GetFromJsonAsync<OrderDto>(url);
        Assert.Equal(10m, first!.Amount);

        // Mutate the row directly, bypassing the application entirely.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MachtenDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE Orders SET TotalAmount = 999 WHERE Id = {0}", created.OrderId);
        }

        var second = await client.GetFromJsonAsync<OrderDto>(url);

        Assert.Equal(10m, second!.Amount);
    }

    /// <summary>
    /// The "writes" limiter permits 100 requests per minute with a queue of 10,
    /// so a burst past that must start returning 429.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_IsRateLimited_UnderABurst()
    {
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 140; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/orders",
                new PlaceOrderCommand($"burst-{i}", 1m, "USD"));
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }
}
