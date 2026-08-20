using FastEndpoints;
using FluentValidation;
using MACHTEN.Application.Features.Money;

namespace MACHTEN.Api.Features.Money;

public sealed class FormatMoneyValidator : Validator<FormatMoneyCommand>
{
    public FormatMoneyValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3).WithMessage("Currency must be a 3-letter ISO 4217 code.");
    }
}
