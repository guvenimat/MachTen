using System.Net;
using System.Net.Http.Json;
using MACHTEN.Application.Features.Money;

namespace MACHTEN.Api.IntegrationTests.Features;

public class FormatMoneyEndpointTests(MachtenApiFactory factory) : IClassFixture<MachtenApiFactory>
{
    [Fact]
    public async Task Post_Money_NormalisesAndFormats()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/money", new FormatMoneyCommand(19.99m, "try"));
        var body = await response.Content.ReadFromJsonAsync<MoneyDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("TRY", body?.Currency);
        Assert.Equal("19.99 TRY", body?.Formatted);
    }

    [Fact]
    public async Task Post_Money_ReturnsBadRequest_ForInvalidCurrency()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/money", new FormatMoneyCommand(10m, "TRYX"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
