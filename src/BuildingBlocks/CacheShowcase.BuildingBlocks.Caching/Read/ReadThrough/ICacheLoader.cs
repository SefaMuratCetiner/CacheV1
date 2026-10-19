namespace CacheShowcase.BuildingBlocks.Caching.Read.ReadThrough;

/// <summary>
/// Loads one value from the source for a read-through cache. Registered once, with the cache, instead of being
/// passed in by every caller.
/// </summary>
public interface ICacheLoader<in TKey, TValue>
    where TValue : class
{
    /// <returns>The value, or null for "not found" (negatively cached when the namespace allows it).</returns>
    Task<TValue?> LoadAsync(TKey key, CancellationToken cancellationToken);
}
