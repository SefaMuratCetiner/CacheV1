using CacheShowcase.BuildingBlocks.Caching.Invalidation.Leases;
using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.Modules.Inventory.Data;
using CacheShowcase.Modules.Inventory.Domain;

namespace CacheShowcase.Modules.Inventory.Caching;

/// <summary>
/// Stock changes often and is read concurrently, which is exactly where the stale-set race bites: a reader loads the
/// old quantity, a write invalidates the key, then the reader stores the old quantity. Leases prevent that: the
/// invalidation revokes the reader's lease, so its store is refused.
/// </summary>
/// <remarks>
/// Writes invalidate directly here, so the caller's next read sees its own write. <see cref="StockChangeListener"/>
/// invalidates again for every change, including ones made outside this API.
/// </remarks>
internal sealed class CachedStockStore(
    StockRepository repository,
    ILeasedCache cache,
    RequestCacheStatus cacheStatus) : IStockStore
{
    public async Task<StockLevel?> GetAsync(int productId, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(
            InventoryCacheKeys.Stock(productId),
            ct => repository.GetAsync(productId, ct),
            cancellationToken);

        cacheStatus.Record(result.Outcome);
        return result.Value;
    }

    public async Task<StockAdjustmentResult> AdjustAsync(int productId, int delta, CancellationToken cancellationToken)
    {
        var result = await repository.AdjustAsync(productId, delta, cancellationToken);
        if (result.Status == StockAdjustmentStatus.Adjusted)
        {
            await cache.InvalidateAsync(InventoryCacheKeys.Stock(productId), cancellationToken);
        }

        return result;
    }
}
