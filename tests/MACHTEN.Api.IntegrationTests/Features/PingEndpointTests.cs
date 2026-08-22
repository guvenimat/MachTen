using System.Net;
using System.Net.Http.Json;
using MACHTEN.Api.Features.Ping;

namespace MACHTEN.Api.IntegrationTests.Features;

public class PingEndpointTests
{
    [ClassDataSource<MachtenApiFactory>(Shared = SharedType.PerTestSession)]
    public required MachtenApiFactory Factory { get; init; }

    [Test]
    public async Task Get_Ping_ReturnsPong()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/ping");
        var body = await response.Content.ReadFromJsonAsync<PingResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body?.Message).IsEqualTo("pong");
    }
}
