namespace MACHTEN.Infrastructure.Repositories;

/// <summary>
/// Placeholder demonstrating where the repository pattern would live in a traditional
/// Clean Architecture setup. In MACHTEN's Vertical Slice approach, data access lives
/// directly in handlers/endpoints via injected DbContext + HybridCache, making this
/// layer optional. Kept here as a reference point for teams that prefer the repository
/// abstraction for complex aggregate persistence logic.
/// </summary>
public sealed class CachedProductRepository
{
    // Intentionally empty -- vertical slices own their data access.
}
