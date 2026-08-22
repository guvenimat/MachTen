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
public class AuthEndpointTests
{
    [ClassDataSource<MachtenApiFactory>(Shared = SharedType.PerTestSession)]
    public required MachtenApiFactory Factory { get; init; }

    [Test]
    public async Task Me_ReturnsUnauthorized_WithoutToken()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/me");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ClientCredentials_IssuesTokenAcceptedByProtectedEndpoint()
    {
        var client = Factory.CreateClient();

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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body?.Subject).IsEqualTo(MachtenApiFactory.ClientId);
    }

    [Test]
    public async Task PasswordGrant_IssuesTokenForSeededIdentityUser()
    {
        var client = Factory.CreateClient();

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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body?.Name).IsEqualTo(MachtenApiFactory.UserEmail);
        await Assert.That(Guid.TryParse(body?.Subject, out _)).IsTrue()
            .Because("Subject should be the Identity user id.");
    }

    [Test]
    public async Task PasswordGrant_IsRejected_ForWrongPassword()
    {
        var client = Factory.CreateClient();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = MachtenApiFactory.UserEmail,
            ["password"] = "definitely-not-the-password",
            ["client_id"] = MachtenApiFactory.ClientId,
            ["client_secret"] = MachtenApiFactory.ClientSecret
        });

        var response = await client.PostAsync("/connect/token", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    private static async Task<string> GetTokenAsync(HttpClient client, Dictionary<string, string> form)
    {
        using var content = new FormUrlEncodedContent(form);
        var response = await client.PostAsync("/connect/token", content);
        var payload = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK)
            .Because($"Token request failed: {payload}");

        using var json = JsonDocument.Parse(payload);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }
}
