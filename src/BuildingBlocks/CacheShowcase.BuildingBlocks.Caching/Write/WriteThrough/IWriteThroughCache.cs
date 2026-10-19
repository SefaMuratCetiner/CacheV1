using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Write.UpdateOnWrite;

namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteThrough;

/// <summary>
/// Write-through: a write goes to the source and then, as part of the same operation, into the cache, so the next
/// read is a fresh hit (read-your-writes). The cache step uses a versioned compare-and-set, so concurrent writers
/// can't leave an older value cached.
/// </summary>
public interface IWriteThroughCache
{
    /// <param name="key">The entry the written value is cached under.</param>
    /// <param name="writeToSource">Writes the source of truth and returns the stored value, or null when nothing was written.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<T?> WriteAsync<T>(CacheKey key, Func<CancellationToken, Task<T?>> writeToSource, CancellationToken cancellationToken = default)
        where T : class, IVersioned;
}
