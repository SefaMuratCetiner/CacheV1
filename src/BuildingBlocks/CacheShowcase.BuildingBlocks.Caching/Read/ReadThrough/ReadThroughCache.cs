using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;

namespace CacheShowcase.BuildingBlocks.Caching.Read.ReadThrough;

/// <inheritdoc cref="IReadThroughCache{TKey,TValue}"/>
/// <remarks>Built on the cache-aside engine, so it shares its stampede protection, negative caching and jitter.</remarks>
internal sealed class ReadThroughCache<TKey, TValue>(
    CacheAsideCache engine,
    ICacheLoader<TKey, TValue> loader,
    Func<TKey, CacheKey> keyFor) : IReadThroughCache<TKey, TValue>
    where TValue : class
{
    public Task<CacheResult<TValue>> GetAsync(TKey key, CancellationToken cancellationToken = default) =>
        engine.GetOrCreateAsync(
            keyFor(key),
            (_, ct) => loader.LoadAsync(key, ct),
            CachePatterns.ReadThrough,
            cancellationToken);
}
