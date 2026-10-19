using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.Sliding;

/// <summary>
/// Sliding expiration: every read pushes the expiry back by <c>SlidingTtl</c>, so entries in use stay cached and
/// idle ones drop out. <c>Ttl</c> caps the total lifetime, because a purely sliding entry that is read often would
/// otherwise never be refreshed. "Not found" is not cached. Requires <c>SlidingTtl</c> on the namespace.
/// </summary>
public interface ISlidingCache
{
    Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class;
}
