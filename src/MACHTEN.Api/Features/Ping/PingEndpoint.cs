using FastEndpoints;

namespace MACHTEN.Api.Features.Ping;

public sealed class PingEndpoint : EndpointWithoutRequest<PingResponse>
{
    public override void Configure()
    {
        Get("/ping");
        Version(1);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Health/liveness sample endpoint";
            s.Description = "Minimal example showing the FastEndpoints + versioning pattern used by this template.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new PingResponse("pong", DateTimeOffset.UtcNow), ct);
    }
}

public sealed record PingResponse(string Message, DateTimeOffset TimestampUtc);
