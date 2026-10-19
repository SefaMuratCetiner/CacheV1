namespace CacheShowcase.BuildingBlocks.Caching.Read.ReadThrough;

/// <summary>
/// Read-through: callers ask only the cache. On a miss the cache itself calls the registered
/// <see cref="ICacheLoader{TKey,TValue}"/>. Same runtime behaviour as cache-aside; the difference is that the
/// loading logic lives with the cache, so every call site is one line and loads the same way.
/// </summary>
public interface IReadThroughCache<in TKey, TValue>
    where TValue : class
{
    Task<CacheResult<TValue>> GetAsync(TKey key, CancellationToken cancellationToken = default);
}
