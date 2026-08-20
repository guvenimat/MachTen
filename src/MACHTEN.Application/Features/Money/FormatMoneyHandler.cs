using MACHTEN.Application.Mapping;

namespace MACHTEN.Application.Features.Money;

public static class FormatMoneyHandler
{
    public static MoneyDto Handle(FormatMoneyCommand command)
    {
        // The domain type owns the rules (non-negative, ISO currency, casing);
        // the handler only translates between the wire shape and the domain.
        var money = new Domain.ValueObjects.Money(command.Amount, command.Currency);

        return MoneyMapper.ToDto(money);
    }
}
