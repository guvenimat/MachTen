using FastEndpoints;
using OpenIddict.Abstractions;

namespace MACHTEN.Api.Features.Auth;

/// <summary>
/// Protected sample endpoint — proves the JWT Bearer handler validates the
/// tokens OpenIddict issues. Every other endpoint opts out with AllowAnonymous.
/// </summary>
public sealed class MeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/me");
        Version(1);
        Summary(s =>
        {
            s.Summary = "Return the caller's identity";
            s.Description = "Requires a bearer token obtained from POST /connect/token.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var subject = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value ?? string.Empty;
        var name = User.FindFirst(OpenIddictConstants.Claims.Name)?.Value ?? string.Empty;
        var scopes = User.FindAll(OpenIddictConstants.Claims.Private.Scope).Select(c => c.Value).ToArray();

        await Send.OkAsync(new MeResponse(subject, name, scopes), ct);
    }
}

public sealed record MeResponse(string Subject, string Name, string[] Scopes);
