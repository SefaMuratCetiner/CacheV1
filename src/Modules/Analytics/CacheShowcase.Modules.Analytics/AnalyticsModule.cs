using System.Reflection;
using CacheShowcase.Modules.Analytics.Caching;
using CacheShowcase.Modules.Analytics.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.Analytics;

/// <summary>
/// Analytics: high-volume writes and a slow aggregate read. Demonstrates write-behind, TTL-only invalidation and
/// probabilistic early expiration (XFetch).
/// </summary>
public static class AnalyticsModule
{
    public static Assembly Assembly => typeof(AnalyticsModule).Assembly;

    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddCacheNamespace(AnalyticsCacheKeys.TrendingNamespace, p =>
        {
            p.Ttl = TimeSpan.FromSeconds(20);
            p.EarlyExpirationBeta = 1.0;
        });

        services.AddSingleton<ProductViewRepository>();

        // Buffered views reach the database every 10 seconds; that is also the most that can be lost.
        services.AddWriteBehindCounter<ProductViewSink>(AnalyticsCacheKeys.ProductViewsBuffer, TimeSpan.FromSeconds(10));

        services.AddScoped<IProductViewStore, CachedProductViewStore>();
        return services;
    }
}
