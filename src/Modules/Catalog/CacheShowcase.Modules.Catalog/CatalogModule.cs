using System.Reflection;
using CacheShowcase.Modules.Catalog.Caching;
using CacheShowcase.Modules.Catalog.Contracts;
using CacheShowcase.Modules.Catalog.Data;
using CacheShowcase.Modules.Catalog.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.Catalog;

/// <summary>
/// Catalog: products and their images. Demonstrates cache-aside, negative caching, id-list caching with generations,
/// write-around with key delete and delayed double delete, immutable content-addressed keys, and publishing events
/// for event-driven invalidation in other modules.
/// </summary>
public static class CatalogModule
{
    public static Assembly Assembly => typeof(CatalogModule).Assembly;

    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddCacheNamespace(CatalogCacheKeys.ProductNamespace, p =>
        {
            p.Ttl = TimeSpan.FromMinutes(5);
            p.NegativeTtl = TimeSpan.FromSeconds(30);
        });
        services.AddCacheNamespace(CatalogCacheKeys.IdPageNamespace, p =>
        {
            p.Ttl = TimeSpan.FromMinutes(1);
            p.NegativeTtl = TimeSpan.FromSeconds(10);
        });
        services.AddCacheNamespace(CatalogCacheKeys.ImageNamespace, p =>
        {
            // Content never changes under a key, so the TTL only bounds memory; LRU eviction does the rest.
            p.Ttl = TimeSpan.FromDays(1);
            p.NegativeTtl = TimeSpan.FromMinutes(1);
        });

        services.AddSingleton<ProductRepository>();

        // Decorators: endpoints depend on the interfaces and never see the cache.
        // Scoped, because the reader reports outcomes to the per-request X-Cache status.
        services.AddScoped<IProductReader>(sp => ActivatorUtilities.CreateInstance<CachedProductReader>(
            sp, sp.GetRequiredService<ProductRepository>()));
        services.AddScoped<IProductWriter>(sp => ActivatorUtilities.CreateInstance<CachedProductWriter>(
            sp, sp.GetRequiredService<ProductRepository>()));

        // Public contract for other modules.
        services.AddSingleton<ICatalogStatistics, CatalogStatistics>();

        return services;
    }
}
