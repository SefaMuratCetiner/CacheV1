using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.Modules.Analytics.Caching;

internal static class AnalyticsCacheKeys
{
    /// <summary>Top products by views (TTL-only + probabilistic early expiration): <c>analytics.trending:v1:{limit}</c>.</summary>
    public const string TrendingNamespace = "analytics.trending";

    /// <summary>Name of the write-behind buffer for product views (Valkey keys and metrics).</summary>
    public const string ProductViewsBuffer = "analytics.product-views";

    public static CacheKey Trending(int limit) => CacheKey.Create(TrendingNamespace, 1, limit);
}
