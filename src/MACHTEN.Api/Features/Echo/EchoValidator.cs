using FastEndpoints;
using FluentValidation;
using MACHTEN.Application.Features.Echo;

namespace MACHTEN.Api.Features.Echo;

public sealed class EchoValidator : Validator<EchoCommand>
{
    public EchoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(500);
    }
}
