namespace MACHTEN.Application.Contracts;

/// <summary>
/// Cache keys live in one place so a write path can never invalidate a key
/// that a read path spells differently.
/// </summary>
public static class CacheKeys
{
    public static string Order(Guid orderId) => $"order:{orderId}";
}
