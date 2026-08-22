using System.Buffers;
using System.Text;
using System.Text.Json;
using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace MACHTEN.Api.IntegrationTests.Features;

/// <summary>
/// Guards the cache serializer choice. HybridCache silently falls back to
/// reflection-based JSON when no serializer claims a type, so "caching works"
/// tests pass either way — only the bytes tell you which one ran.
/// </summary>
public class CacheSerializerTests
{
    private static readonly OrderDto Sample = new(
        Guid.CreateVersion7(), "acme-42", 19.99m, "TRY", "19.99 TRY", "Placed", DateTimeOffset.UtcNow);

    [Fact]
    public void RoundTrip_PreservesEveryField()
    {
        var serializer = new MemoryPackCacheSerializer<OrderDto>();

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(Sample, buffer);
        var restored = serializer.Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

        Assert.Equal(Sample, restored);
    }

    [Fact]
    public void Output_IsBinary_AndSmallerThanJson()
    {
        var serializer = new MemoryPackCacheSerializer<OrderDto>();

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(Sample, buffer);
        var bytes = buffer.WrittenSpan;

        var json = JsonSerializer.SerializeToUtf8Bytes(Sample);

        // JSON is entirely printable and opens with '{'. Binary output is
        // neither, which is the cheapest way to prove JSON did not run.
        Assert.NotEqual((byte)'{', bytes[0]);
        Assert.True(
            bytes.Length < json.Length,
            $"MemoryPack {bytes.Length}B should undercut JSON {json.Length}B ({Encoding.UTF8.GetString(json)})");
    }
}

/// <summary>
/// The registration is the part that failed silently the first time: a
/// serializer factory that declined every type, so HybridCache fell back to
/// JSON while every caching test still passed.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class CacheSerializerRegistrationTests(MachtenApiFactory factory)
{
    [Fact]
    public void HybridCache_ResolvesMemoryPackForOrderDto()
    {
        using var scope = factory.Services.CreateScope();

        var serializer = scope.ServiceProvider.GetRequiredService<IHybridCacheSerializer<OrderDto>>();

        Assert.IsType<MemoryPackCacheSerializer<OrderDto>>(serializer);
    }
}
