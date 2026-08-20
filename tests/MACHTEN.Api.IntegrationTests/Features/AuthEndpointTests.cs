using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MACHTEN.Api.Features.Auth;

namespace MACHTEN.Api.IntegrationTests.Features;

/// <summary>
/// Exercises the full loop: OpenIddict issues a token at /connect/token and the
/// JWT Bearer handler validates it on a protected endpoint.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AuthEndpointTests(MachtenApiFactory factory)
{
    [Fact]
    public async Task Me_ReturnsUnauthorized_WithoutToken()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClientCredentials_IssuesTokenAcceptedByProtectedEndpoint()
    {
        var client = factory.CreateClient();

        var token = await GetTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = MachtenApiFactory.ClientId,
            ["client_secret"] = MachtenApiFactory.ClientSecret,
            ["scope"] = "api"
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/me");
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MachtenApiFactory.ClientId, body?.Subject);
    }

    [Fact]
    public async Task PasswordGrant_IssuesTokenForSeededIdentityUser()
    {
        var client = factory.CreateClient();

        var token = await GetTokenAsync(client, new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = MachtenApiFactory.UserEmail,
            ["password"] = MachtenApiFactory.UserPassword,
            ["client_id"] = MachtenApiFactory.ClientId,
            ["client_secret"] = MachtenApiFactory.ClientSecret,
            ["scope"] = "api"
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/me");
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MachtenApiFactory.UserEmail, body?.Name);
        Assert.True(Guid.TryParse(body?.Subject, out _), "Subject should be the Identity user id.");
    }

    [Fact]
    public async Task PasswordGrant_IsRejected_ForWrongPassword()
    {
        var client = factory.CreateClient();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = MachtenApiFactory.UserEmail,
            ["password"] = "definitely-not-the-password",
            ["client_id"] = MachtenApiFactory.ClientId,
            ["client_secret"] = MachtenApiFactory.ClientSecret
        });

        var response = await client.PostAsync("/connect/token", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<string> GetTokenAsync(HttpClient client, Dictionary<string, string> form)
    {
        using var content = new FormUrlEncodedContent(form);
        var response = await client.PostAsync("/connect/token", content);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Token request failed ({response.StatusCode}): {payload}");

        using var json = JsonDocument.Parse(payload);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }
}
