using FastEndpoints;
using MACHTEN.Application.Features.Money;

namespace MACHTEN.Api.Features.Money;

public sealed class FormatMoneyEndpoint : Endpoint<FormatMoneyCommand, MoneyDto>
{
    public override void Configure()
    {
        Post("/money");
        Version(1);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Normalise and format a monetary value";
            s.Description = "Sample slice showing request validation, a domain value object and Mapperly-generated mapping.";
        });
    }

    public override async Task HandleAsync(FormatMoneyCommand req, CancellationToken ct)
    {
        var result = FormatMoneyHandler.Handle(req);
        await Send.OkAsync(result, ct);
    }
}
