using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.Leases;

/// <summary>
/// Cache-aside with leases (after Facebook's memcache leases). On a miss a caller must win a short lease before
/// loading; only the lease holder may store the value, and invalidation revokes the lease. This gives:
/// <list type="bullet">
/// <item>No stale sets: a load that started before an invalidation can't store its now-stale value.</item>
/// <item>Cross-instance stampede protection: other callers, on any instance, wait briefly for the holder instead of
/// all loading.</item>
/// </list>
/// </summary>
public interface ILeasedCache
{
    Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Deletes the entry and revokes any outstanding lease in one command.</summary>
    Task InvalidateAsync(CacheKey key, CancellationToken cancellationToken = default);
}
