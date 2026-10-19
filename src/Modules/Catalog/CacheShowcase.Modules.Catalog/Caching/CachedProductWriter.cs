using CacheShowcase.BuildingBlocks.Caching.Invalidation.DoubleDelete;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.Generations;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteAround;
using CacheShowcase.Modules.Catalog.Data;
using CacheShowcase.Modules.Catalog.Domain;

namespace CacheShowcase.Modules.Catalog.Caching;

/// <summary>
/// Decorator that keeps Catalog's own caches correct after writes. Both writes are write-around: they go to the
/// database only, and the cache is refilled by the next read.
/// </summary>
/// <remarks>
/// Within a module, invalidation is direct (this class). Across modules it is event-driven: endpoints publish
/// <c>ProductChanged</c> and other modules clean up their own caches.
/// </remarks>
internal sealed class CachedProductWriter(
    IProductWriter inner,
    IWriteAround writeAround,
    IDoubleDelete doubleDelete,
    ICacheGenerations generations) : IProductWriter
{
    /// <summary>
    /// Write-around + key delete. A new product can only be stale in the cache as a cached "not found" for its id
    /// (negative caching), so that key is deleted. List pages may now include it, so their generation advances.
    /// </summary>
    public async Task<Product> CreateAsync(NewProduct product, CancellationToken cancellationToken)
    {
        var created = await writeAround.WriteAsync(
            ct => inner.CreateAsync(product, ct),
            p => [CatalogCacheKeys.ById(p.Id)],
            cancellationToken);

        await generations.AdvanceAsync(CatalogCacheKeys.IdPageNamespace, cancellationToken);
        return created;
    }

    /// <summary>
    /// Write-around + delayed double delete: the product's entry is deleted before the update and again shortly
    /// after, removing a stale value a concurrent reader may have stored in between. The category may have changed,
    /// so list pages are invalidated too.
    /// </summary>
    public async Task<ProductUpdateResult> UpdateAsync(ProductUpdate update, CancellationToken cancellationToken)
    {
        var result = await doubleDelete.WriteAsync(
            [CatalogCacheKeys.ById(update.Id)],
            ct => inner.UpdateAsync(update, ct),
            cancellationToken);

        if (result.Status == ProductUpdateStatus.Updated)
        {
            await generations.AdvanceAsync(CatalogCacheKeys.IdPageNamespace, cancellationToken);
        }

        return result;
    }
}
