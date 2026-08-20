using System.Security.Claims;
using MACHTEN.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace MACHTEN.Api.Features.Auth;

/// <summary>
/// OpenIddict token endpoint. Mapped as a minimal API rather than a
/// FastEndpoint because it must sit at /connect/token, outside the "api"
/// route prefix that OpenIddict advertises in its server configuration.
/// </summary>
public static class TokenEndpoint
{
    public static void MapTokenEndpoint(this WebApplication app)
    {
        app.MapPost("/connect/token", async (
            HttpContext http,
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn) =>
        {
            var request = http.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsClientCredentialsGrantType())
            {
                // Machine-to-machine: the client itself is the subject.
                var identity = new ClaimsIdentity(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    Claims.Name,
                    Claims.Role);

                identity.SetClaim(Claims.Subject, request.ClientId);
                identity.SetClaim(Claims.Name, request.ClientId);
                identity.SetScopes(request.GetScopes());
                identity.SetDestinations(static _ => [Destinations.AccessToken]);

                return Results.SignIn(
                    new ClaimsPrincipal(identity),
                    properties: null,
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (request.IsPasswordGrantType())
            {
                var user = await users.FindByNameAsync(request.Username!);
                if (user is null)
                    return InvalidGrant("The username/password couple is invalid.");

                var result = await signIn.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
                if (!result.Succeeded)
                    return InvalidGrant("The username/password couple is invalid.");

                var principal = await signIn.CreateUserPrincipalAsync(user);
                principal.SetScopes(request.GetScopes());
                principal.SetDestinations(GetDestinations);

                return Results.SignIn(
                    principal,
                    properties: null,
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (request.IsRefreshTokenGrantType())
            {
                var auth = await http.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var principal = auth.Principal;

                if (principal is null)
                    return InvalidGrant("The refresh token is no longer valid.");

                var user = await users.GetUserAsync(principal);
                if (user is null || !await signIn.CanSignInAsync(user))
                    return InvalidGrant("The user is no longer allowed to sign in.");

                var refreshed = await signIn.CreateUserPrincipalAsync(user);
                refreshed.SetScopes(principal.GetScopes());
                refreshed.SetDestinations(GetDestinations);

                return Results.SignIn(
                    refreshed,
                    properties: null,
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return InvalidGrant("The specified grant type is not supported.");
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .ExcludeFromDescription();
    }

    private static IResult InvalidGrant(string description) => Results.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }),
        [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

    /// <summary>
    /// Decides which token each claim is allowed to travel in. Claims default to
    /// the access token only; identity claims also go to the identity token when
    /// the matching scope was granted.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name or Claims.PreferredUsername:
                yield return Destinations.AccessToken;
                if (claim.Subject!.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (claim.Subject!.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                if (claim.Subject!.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;
                yield break;

            // Never expose the security stamp.
            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
