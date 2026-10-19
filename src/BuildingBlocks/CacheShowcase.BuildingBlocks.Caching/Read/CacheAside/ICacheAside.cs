using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;

/// <summary>
/// Cache-aside (lazy loading): look in the cache; on a miss, load from the source with the caller's factory,
/// store the result and return it. Includes single-flight stampede protection, negative caching and TTL jitter.
/// </summary>
public interface ICacheAside
{
    /// <param name="key">The entry's key; its namespace selects the TTL policy.</param>
    /// <param name="factory">Loads the value from the source. Return null for "not found" (negatively cached).</param>
    /// <param name="cancellationToken">Stops this caller waiting; a shared load keeps running for other callers.</param>
    Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Batch cache-aside: one <c>MGET</c> for all keys, one factory call for all misses, one pipelined write.
    /// Cached "not found" entries count as misses here.
    /// </summary>
    Task<CacheBatchResult<TId, T>> GetOrCreateManyAsync<TId, T>(
        IReadOnlyCollection<TId> ids,
        Func<TId, CacheKey> keyFor,
        Func<IReadOnlyCollection<TId>, CancellationToken, Task<IReadOnlyDictionary<TId, T>>> factory,
        CancellationToken cancellationToken = default)
        where TId : notnull
        where T : class;
}
