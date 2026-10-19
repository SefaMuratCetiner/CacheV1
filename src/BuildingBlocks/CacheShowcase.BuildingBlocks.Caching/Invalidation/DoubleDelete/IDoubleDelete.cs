using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.DoubleDelete;

/// <summary>
/// Delayed double delete: delete the entries, write the source, then delete them again after a short delay.
/// The second delete removes a stale value that a concurrent reader loaded from the database before the write
/// committed and stored after the first delete (the stale-set race). Cheap, but probabilistic: a reader slower than
/// the delay can still win; leases close that gap fully.
/// </summary>
public interface IDoubleDelete
{
    Task<T> WriteAsync<T>(
        IReadOnlyCollection<CacheKey> keys,
        Func<CancellationToken, Task<T>> writeToSource,
        CancellationToken cancellationToken = default);
}
