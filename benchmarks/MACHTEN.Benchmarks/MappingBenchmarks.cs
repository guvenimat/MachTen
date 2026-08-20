using System.Reflection;
using BenchmarkDotNet.Attributes;
using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Application.Mapping;
using MACHTEN.Domain.Entities;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Benchmarks;

/// <summary>
/// Backs the "compile-time mapping, no reflection" claim with numbers.
/// The reflection baseline stands in for the convention-based mappers this
/// template deliberately avoids.
/// </summary>
[MemoryDiagnoser]
public class MappingBenchmarks
{
    private Order _order = null!;
    private static readonly PropertyInfo[] DtoProperties = typeof(OrderDto).GetProperties();

    [GlobalSetup]
    public void Setup() => _order = Order.Place("acme-42", new Money(19.99m, "TRY"));

    [Benchmark(Baseline = true, Description = "Mapperly (source generated)")]
    public OrderDto Mapperly() => OrderMapper.ToDto(_order);

    [Benchmark(Description = "Reflection")]
    public OrderDto Reflection()
    {
        var values = new object?[DtoProperties.Length];

        for (var i = 0; i < DtoProperties.Length; i++)
        {
            values[i] = DtoProperties[i].Name switch
            {
                nameof(OrderDto.Id) => typeof(Order).GetProperty(nameof(Order.Id))!.GetValue(_order),
                nameof(OrderDto.CustomerReference) => typeof(Order).GetProperty(nameof(Order.CustomerReference))!.GetValue(_order),
                nameof(OrderDto.Amount) => typeof(Money).GetProperty(nameof(Money.Amount))!.GetValue(_order.Total),
                nameof(OrderDto.Currency) => typeof(Money).GetProperty(nameof(Money.Currency))!.GetValue(_order.Total),
                nameof(OrderDto.Formatted) => typeof(Money).GetProperty(nameof(Money.Formatted))!.GetValue(_order.Total),
                nameof(OrderDto.Status) => typeof(Order).GetProperty(nameof(Order.Status))!.GetValue(_order)!.ToString(),
                nameof(OrderDto.CreatedAtUtc) => typeof(Order).GetProperty(nameof(Order.CreatedAtUtc))!.GetValue(_order),
                _ => null
            };
        }

        return (OrderDto)Activator.CreateInstance(typeof(OrderDto), values)!;
    }
}
