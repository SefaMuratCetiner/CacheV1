using System.Reflection;
using CacheShowcase.Modules.Inventory.Caching;
using CacheShowcase.Modules.Inventory.Contracts;
using CacheShowcase.Modules.Inventory.Data;
using CacheShowcase.Modules.Inventory.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.Inventory;

/// <summary>
/// Inventory: volatile data under concurrent reads and writes. Demonstrates leases (no stale sets, cross-instance
/// stampede protection) and change capture (invalidation driven by the database, not the writer).
/// </summary>
public static class InventoryModule
{
    public static Assembly Assembly => typeof(InventoryModule).Assembly;

    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddCacheNamespace(InventoryCacheKeys.StockNamespace, p =>
        {
            // Short: every change is invalidated, so the TTL only covers a missed notification.
            p.Ttl = TimeSpan.FromSeconds(30);
            p.NegativeTtl = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<StockRepository>();
        services.AddScoped<IStockStore, CachedStockStore>();
        services.AddHostedService<StockChangeListener>();

        services.AddSingleton<IInventoryStatistics, InventoryStatistics>();
        return services;
    }
}
