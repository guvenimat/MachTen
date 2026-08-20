using System.Net;
using System.Net.Http.Json;
using MACHTEN.Api.Features.Ping;

namespace MACHTEN.Api.IntegrationTests.Features;

public class PingEndpointTests(MachtenApiFactory factory) : IClassFixture<MachtenApiFactory>
{
    [Fact]
    public async Task Get_Ping_ReturnsPong()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/ping");
        var body = await response.Content.ReadFromJsonAsync<PingResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", body?.Message);
    }
}
