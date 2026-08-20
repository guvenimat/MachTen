namespace MACHTEN.Application.Features.Money;

public sealed record FormatMoneyCommand(decimal Amount, string Currency);
