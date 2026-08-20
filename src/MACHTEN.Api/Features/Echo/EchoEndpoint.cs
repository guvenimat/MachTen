using FastEndpoints;
using MACHTEN.Application.Features.Echo;

namespace MACHTEN.Api.Features.Echo;

public sealed class EchoEndpoint : Endpoint<EchoCommand, EchoResponse>
{
    public override void Configure()
    {
        Post("/echo");
        Version(1);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Validated request/response sample endpoint";
            s.Description = "Minimal example showing FastEndpoints request validation via FluentValidation.";
        });
    }

    public override async Task HandleAsync(EchoCommand req, CancellationToken ct)
    {
        var result = EchoHandler.Handle(req);
        await Send.OkAsync(result, ct);
    }
}
