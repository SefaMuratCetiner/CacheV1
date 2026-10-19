using CacheShowcase.BuildingBlocks.Caching;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.Generations;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;
using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.Modules.Catalog.Data;
using CacheShowcase.Modules.Catalog.Domain;

namespace CacheShowcase.Modules.Catalog.Caching;

/// <summary>
/// Decorator that adds cache-aside reads to <see cref="IProductReader"/>. Endpoints can't tell it apart from the
/// repository.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><b>By id</b>: cache-aside, including "not found" (negative caching).</item>
/// <item><b>Lists</b>: id-list caching. A page caches only ids; the products come from their own entries through one
/// batched <c>MGET</c>, so an updated product is fresh in every list at once. Page keys embed a generation, so one
/// increment invalidates every page.</item>
/// <item><b>Images</b>: content-addressed, so never invalidated, only evicted or expired.</item>
/// </list>
/// </remarks>
internal sealed class CachedProductReader(
    IProductReader inner,
    ICacheAside cache,
    ICacheGenerations generations,
    RequestCacheStatus cacheStatus) : IProductReader
{
    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(CatalogCacheKeys.ById(id), (_, ct) => inner.GetByIdAsync(id, ct), cancellationToken);
        cacheStatus.Record(result.Outcome);
        return result.Value;
    }

    public async Task<IReadOnlyDictionary<int, Product>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateManyAsync(ids, CatalogCacheKeys.ById, inner.GetManyAsync, cancellationToken);
        cacheStatus.Record(result);
        return result.Values;
    }

    public async Task<ProductIdPage> FindIdsAsync(ProductFilter filter, CancellationToken cancellationToken)
    {
        var generation = await generations.GetAsync(CatalogCacheKeys.IdPageNamespace, cancellationToken);
        if (generation is null)
        {
            // Without the generation a current page can't be told from an invalidated one, so skip the cache.
            cacheStatus.Record(CacheOutcome.Bypass);
            return await inner.FindIdsAsync(filter, cancellationToken);
        }

        var result = await cache.GetOrCreateAsync(
            CatalogCacheKeys.IdPage(generation.Value, filter),
            async (_, ct) => await inner.FindIdsAsync(filter, ct),
            cancellationToken);

        cacheStatus.Record(result.Outcome);
        return result.Value ?? throw new InvalidOperationException("An id page is never cached as missing.");
    }

    public async Task<ProductImage?> GetImageAsync(string hash, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(CatalogCacheKeys.Image(hash), (_, ct) => inner.GetImageAsync(hash, ct), cancellationToken);
        cacheStatus.Record(result.Outcome);
        return result.Value;
    }
}
