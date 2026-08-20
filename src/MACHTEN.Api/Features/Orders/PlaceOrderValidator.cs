using FastEndpoints;
using FluentValidation;
using MACHTEN.Application.Features.Orders.PlaceOrder;

namespace MACHTEN.Api.Features.Orders;

public sealed class PlaceOrderValidator : Validator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerReference)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3).WithMessage("Currency must be a 3-letter ISO 4217 code.");
    }
}
