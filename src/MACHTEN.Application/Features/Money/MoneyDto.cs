namespace MACHTEN.Application.Features.Money;

public sealed record MoneyDto(decimal Amount, string Currency, string Formatted);
