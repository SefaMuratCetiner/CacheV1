using CacheShowcase.BuildingBlocks.Http.OutputCaching;
using CacheShowcase.Modules.Catalog.Contracts;
using CacheShowcase.Modules.Inventory.Contracts;
using FastEndpoints;

namespace CacheShowcase.Modules.Reports.Subscribers;

/// <summary>
/// Event-driven invalidation: Reports caches data it doesn't own, so it subscribes to the owners' change events and
/// evicts its own cached responses by tag. Catalog and Inventory don't know this cache exists.
/// </summary>
public sealed class EvictCatalogOverviewOnProductChanged(OutputCacheTagEviction eviction) : IEventHandler<ProductChanged>
{
    public Task HandleAsync(ProductChanged eventModel, CancellationToken ct) =>
        eviction.EvictAsync(ReportsOutputCache.CatalogDataTag, ct);
}

/// <inheritdoc cref="EvictCatalogOverviewOnProductChanged"/>
public sealed class EvictCatalogOverviewOnStockChanged(OutputCacheTagEviction eviction) : IEventHandler<StockChanged>
{
    public Task HandleAsync(StockChanged eventModel, CancellationToken ct) =>
        eviction.EvictAsync(ReportsOutputCache.CatalogDataTag, ct);
}
