using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;

namespace CacheShowcase.BuildingBlocks.Caching.Stores.TwoLevel;

/// <summary>
/// A near cache: a small in-process L1 in front of the shared Valkey L2. Hot reads cost no network round trip;
/// everything else is still shared by all instances. Requires <c>LocalTtl</c> on the namespace.
/// </summary>
public interface ITwoLevelCache
{
    Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Deletes the entry from L2 and broadcasts the L1 eviction to every instance.</summary>
    Task InvalidateAsync(CacheKey key, CancellationToken cancellationToken = default);
}
