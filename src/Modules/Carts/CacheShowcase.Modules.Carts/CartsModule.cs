using System.Reflection;
using CacheShowcase.Modules.Carts.Caching;
using CacheShowcase.Modules.Carts.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.Carts;

/// <summary>Carts: session-like data. Demonstrates sliding expiration with an absolute cap, plus write-around.</summary>
public static class CartsModule
{
    public static Assembly Assembly => typeof(CartsModule).Assembly;

    public static IServiceCollection AddCartsModule(this IServiceCollection services)
    {
        services.AddCacheNamespace(CartCacheKeys.CartNamespace, p =>
        {
            p.SlidingTtl = TimeSpan.FromMinutes(2);
            p.Ttl = TimeSpan.FromMinutes(30);
        });

        services.AddSingleton<CartRepository>();
        services.AddScoped<ICartStore, CachedCartStore>();
        return services;
    }
}
