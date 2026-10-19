using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.Modules.Inventory.Caching;

internal static class InventoryCacheKeys
{
    /// <summary>Stock for one product (leases): <c>inventory.stock:v1:{productId}</c>, lease at <c>…:lease</c>.</summary>
    public const string StockNamespace = "inventory.stock";

    public static CacheKey Stock(int productId) => CacheKey.Create(StockNamespace, 1, productId);
}
