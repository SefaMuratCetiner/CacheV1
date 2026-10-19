using System.Globalization;
using CacheShowcase.BuildingBlocks.Caching.Expiration.ProbabilisticEarly;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;
using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.Modules.Analytics.Data;

namespace CacheShowcase.Modules.Analytics.Caching;

/// <summary>
/// <list type="bullet">
/// <item><b>Write-behind</b>: a view is one <c>HINCRBY</c> in Valkey; <see cref="ProductViewSink"/> receives the
/// totals in a batch every interval.</item>
/// <item><b>TTL-only + probabilistic early expiration</b>: the trending list may be a few seconds stale, so nothing
/// invalidates it; XFetch lets one early caller recompute the slow query before the entry expires for everyone.</item>
/// </list>
/// Trending figures lag recorded views by up to one flush interval plus the TTL: an accepted trade-off.
/// </summary>
internal sealed class CachedProductViewStore(
    IWriteBehindCounter<ProductViewSink> views,
    ProductViewRepository repository,
    IEarlyExpirationCache cache,
    RequestCacheStatus cacheStatus) : IProductViewStore
{
    public Task RecordViewAsync(int productId, CancellationToken cancellationToken) =>
        views.IncrementAsync(productId.ToString(CultureInfo.InvariantCulture), cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<TrendingProduct>> GetTrendingAsync(int limit, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(
            AnalyticsCacheKeys.Trending(limit),
            async ct => await repository.GetTrendingAsync(limit, ct),
            cancellationToken);

        cacheStatus.Record(result.Outcome);
        return result.Value ?? [];
    }
}
