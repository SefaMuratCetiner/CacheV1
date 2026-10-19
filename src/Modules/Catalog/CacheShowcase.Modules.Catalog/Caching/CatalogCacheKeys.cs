using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.Modules.Catalog.Data;

namespace CacheShowcase.Modules.Catalog.Caching;

/// <summary>Every Catalog cache key is built here. Bump a schema version when the cached shape changes.</summary>
internal static class CatalogCacheKeys
{
    /// <summary>One product by id (cache-aside + negative caching): <c>catalog.product:v1:{id}</c>.</summary>
    public const string ProductNamespace = "catalog.product";

    /// <summary>
    /// One page of product ids (id-list caching under a generation):
    /// <c>catalog.product-ids:v1:g{generation}:{category}:{page}:{pageSize}</c>.
    /// </summary>
    public const string IdPageNamespace = "catalog.product-ids";

    /// <summary>One image by content hash (immutable key): <c>catalog.image:v1:{sha256}</c>.</summary>
    public const string ImageNamespace = "catalog.image";

    // Not a valid category, so it can't collide with one.
    private const string AllCategories = "_all";

    public static CacheKey ById(int id) => CacheKey.Create(ProductNamespace, 1, id);

    public static CacheKey IdPage(long generation, ProductFilter filter) =>
        CacheKey.Create(IdPageNamespace, 1, $"g{generation}", filter.Category ?? AllCategories, filter.Page, filter.PageSize);

    public static CacheKey Image(string hash) => CacheKey.Create(ImageNamespace, 1, hash);
}
