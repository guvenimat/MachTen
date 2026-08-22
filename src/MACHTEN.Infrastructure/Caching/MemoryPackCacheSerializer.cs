using System.Buffers;
using MemoryPack;
using Microsoft.Extensions.Caching.Hybrid;

namespace MACHTEN.Infrastructure.Caching;

/// <summary>
/// Serializes cache entries with MemoryPack instead of HybridCache's default
/// JSON. Formatters are source generated, so nothing is discovered by
/// reflection at runtime — which is what keeps this path viable under trimming
/// and Native AOT.
///
/// Registered explicitly per type in Program.cs rather than through an
/// IHybridCacheSerializerFactory. A factory has to decide whether it can handle
/// a type, and MemoryPackFormatterProvider.IsRegistered&lt;T&gt;() answers false
/// until something has already serialized that type — so the factory declined
/// everything and every cache write quietly fell back to JSON. Listing the
/// types costs a line each and cannot fail silently.
/// </summary>
public sealed class MemoryPackCacheSerializer<T> : IHybridCacheSerializer<T>
{
    public T Deserialize(ReadOnlySequence<byte> source) => MemoryPackSerializer.Deserialize<T>(source)!;

    public void Serialize(T value, IBufferWriter<byte> target) => MemoryPackSerializer.Serialize(target, value);
}
