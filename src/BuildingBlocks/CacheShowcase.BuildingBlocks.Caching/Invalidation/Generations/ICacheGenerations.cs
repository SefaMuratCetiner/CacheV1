namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.Generations;

/// <summary>
/// Generation-based group invalidation: keys of a group embed a counter (<c>…:g7:…</c>). Advancing the counter makes
/// every old key unreachable in one command; the orphaned entries expire through their TTL. Ideal for groups that are
/// hard to enumerate, such as every page of a filtered list.
/// </summary>
public interface ICacheGenerations
{
    /// <returns>The current generation, or null when it can't be read (the caller must then skip the cache).</returns>
    Task<long?> GetAsync(string cacheNamespace, CancellationToken cancellationToken = default);

    Task AdvanceAsync(string cacheNamespace, CancellationToken cancellationToken = default);
}
