using MACHTEN.Application.Features.Money;
using Riok.Mapperly.Abstractions;

namespace MACHTEN.Application.Mapping;

/// <summary>
/// Mapperly generates the body of <see cref="ToDto"/> at compile time — no
/// reflection, no runtime configuration to keep in sync.
/// </summary>
[Mapper]
public static partial class MoneyMapper
{
    public static partial MoneyDto ToDto(Domain.ValueObjects.Money money);
}
