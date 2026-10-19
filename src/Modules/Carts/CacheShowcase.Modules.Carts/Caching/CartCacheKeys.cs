using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.Modules.Carts.Caching;

internal static class CartCacheKeys
{
    /// <summary>One cart (sliding expiration): <c>carts.cart:v1:{cartId}</c>.</summary>
    public const string CartNamespace = "carts.cart";

    public static CacheKey Cart(Guid cartId) => CacheKey.Create(CartNamespace, 1, cartId.ToString("N"));
}
