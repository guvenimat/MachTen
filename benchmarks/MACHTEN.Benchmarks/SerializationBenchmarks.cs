using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using MACHTEN.Application.Features.Orders.GetOrder;

namespace MACHTEN.Benchmarks;

/// <summary>
/// Backs the source-generated JSON claim. The API registers a
/// JsonSerializerContext exactly like the one below; the reflection case is
/// what you get by default without it.
/// </summary>
[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private OrderDto _dto = null!;

    private static readonly JsonSerializerOptions ReflectionOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [GlobalSetup]
    public void Setup() => _dto = new OrderDto(
        Guid.CreateVersion7(), "acme-42", 19.99m, "TRY", "19.99 TRY", "Placed", DateTimeOffset.UtcNow);

    [Benchmark(Baseline = true, Description = "System.Text.Json (source generated)")]
    public string SourceGenerated() => JsonSerializer.Serialize(_dto, BenchmarkJsonContext.Default.OrderDto);

    [Benchmark(Description = "System.Text.Json (reflection)")]
    public string Reflection() => JsonSerializer.Serialize(_dto, ReflectionOptions);
}

[JsonSerializable(typeof(OrderDto))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class BenchmarkJsonContext : JsonSerializerContext;
