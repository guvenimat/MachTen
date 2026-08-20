using System.Net;
using System.Net.Http.Json;
using MACHTEN.Application.Features.Echo;

namespace MACHTEN.Api.IntegrationTests.Features;

public class EchoEndpointTests(MachtenApiFactory factory) : IClassFixture<MachtenApiFactory>
{
    [Fact]
    public async Task Post_Echo_ReturnsMessageAndLength()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/echo", new EchoCommand("merhaba"));
        var body = await response.Content.ReadFromJsonAsync<EchoResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("merhaba", body?.Message);
        Assert.Equal(7, body?.Length);
    }

    [Fact]
    public async Task Post_Echo_ReturnsBadRequest_WhenMessageIsEmpty()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/echo", new EchoCommand(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
