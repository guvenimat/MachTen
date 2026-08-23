using System.Text.Json.Serialization;
using MACHTEN.Application.Contracts.Auth;
using Microsoft.AspNetCore.Identity;

namespace MACHTEN.Api.Features.Auth;

/// <summary>
/// OAuth2 token endpoint, hand-rolled in place of OpenIddict.
///
/// Supports client_credentials and password. There is no refresh flow and no
/// discovery document: dropping OpenIddict traded those away for a surface
/// small enough to read in one sitting and free of reflection.
///
/// Mapped as a minimal API so it sits at /connect/token, outside the "api"
/// route prefix FastEndpoints applies.
/// </summary>
public static class TokenEndpoint
{
    public static void MapTokenEndpoint(this WebApplication app)
    {
        app.MapPost("/connect/token", async (
            HttpRequest request,
            IUserRepository users,
            IApiClientRepository clients,
            ITokenIssuer issuer,
            IPasswordHasher<string> hasher,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
            {
                return InvalidRequest("The token request must be form encoded.");
            }

            var form = await request.ReadFormAsync(ct);
            var grantType = form["grant_type"].ToString();

            return grantType switch
            {
                "client_credentials" => await ClientCredentialsAsync(form, clients, issuer, hasher, ct),
                "password" => await PasswordAsync(form, users, issuer, hasher, ct),
                _ => InvalidRequest($"Unsupported grant_type '{grantType}'.")
            };
        })
        .AllowAnonymous()
        .ExcludeFromDescription();
    }

    private static async Task<IResult> ClientCredentialsAsync(
        IFormCollection form,
        IApiClientRepository clients,
        ITokenIssuer issuer,
        IPasswordHasher<string> hasher,
        CancellationToken ct)
    {
        var clientId = form["client_id"].ToString();
        var secret = form["client_secret"].ToString();

        var client = await clients.FindAsync(clientId, ct);

        // Same response whether the client is unknown or the secret is wrong,
        // so the endpoint cannot be used to enumerate client ids.
        if (client is null ||
            hasher.VerifyHashedPassword(clientId, client.SecretHash, secret) == PasswordVerificationResult.Failed)
        {
            return InvalidClient();
        }

        var (token, expiresIn) = issuer.Issue(client.ClientId, client.ClientId, client.Scopes);
        return Results.Ok(new TokenResponse(token, "Bearer", expiresIn, client.Scopes));
    }

    private static async Task<IResult> PasswordAsync(
        IFormCollection form,
        IUserRepository users,
        ITokenIssuer issuer,
        IPasswordHasher<string> hasher,
        CancellationToken ct)
    {
        var username = form["username"].ToString();
        var password = form["password"].ToString();
        var scope = form["scope"].ToString();

        var user = await users.FindByEmailAsync(username, ct);

        if (user is null ||
            hasher.VerifyHashedPassword(username, user.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            return InvalidGrant("The username/password couple is invalid.");
        }

        var (token, expiresIn) = issuer.Issue(user.Id.ToString(), user.Email, scope);
        return Results.Ok(new TokenResponse(token, "Bearer", expiresIn, scope));
    }

    // RFC 6749 error shapes: 400 for a bad request or grant, 401 for a client
    // that failed to authenticate.
    private static IResult InvalidRequest(string description) =>
        Results.BadRequest(new TokenError("invalid_request", description));

    private static IResult InvalidGrant(string description) =>
        Results.BadRequest(new TokenError("invalid_grant", description));

    private static IResult InvalidClient() =>
        Results.Json(new TokenError("invalid_client", "Client authentication failed."), statusCode: 401);
}

/// <summary>
/// RFC 6749 names the fields in snake_case, and clients depend on that exact
/// spelling — the default camelCase would produce a token response nothing can
/// read.
/// </summary>
public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("scope")] string Scope);

public sealed record TokenError(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription);
