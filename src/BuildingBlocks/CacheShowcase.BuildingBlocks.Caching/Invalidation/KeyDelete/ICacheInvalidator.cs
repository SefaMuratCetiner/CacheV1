using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.KeyDelete;

/// <summary>
/// Explicit invalidation by key: deletes exactly the given entries. Precise, but the writer must know every key
/// that holds the changed data. A failure is logged and the entry's TTL bounds the staleness.
/// </summary>
public interface ICacheInvalidator
{
    Task RemoveAsync(IReadOnlyCollection<CacheKey> keys, CancellationToken cancellationToken = default);
}
