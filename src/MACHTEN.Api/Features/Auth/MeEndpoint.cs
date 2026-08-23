using FastEndpoints;
using MACHTEN.Api.Infrastructure.Auth;

namespace MACHTEN.Api.Features.Auth;

/// <summary>
/// Protected sample endpoint — proves the JWT Bearer handler validates the
/// tokens this API issues. Every other endpoint opts out with AllowAnonymous.
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
        var subject = User.FindFirst(ClaimNames.Subject)?.Value ?? string.Empty;
        var name = User.FindFirst(ClaimNames.Name)?.Value ?? string.Empty;
        var scopes = User.FindAll(ClaimNames.Scope).SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();

        await Send.OkAsync(new MeResponse(subject, name, scopes), ct);
    }
}

public sealed record MeResponse(string Subject, string Name, string[] Scopes);
