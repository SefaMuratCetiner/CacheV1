using System.Reflection;
using CacheShowcase.Modules.ReferenceData.Caching;
using CacheShowcase.Modules.ReferenceData.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.ReferenceData;

/// <summary>
/// Reference data: small, hot, rarely changed. Demonstrates the two-level (near) cache, cache warming, TTL-backed
/// local copies and pub/sub broadcast invalidation.
/// </summary>
public static class ReferenceDataModule
{
    public static Assembly Assembly => typeof(ReferenceDataModule).Assembly;

    public static IServiceCollection AddReferenceDataModule(this IServiceCollection services)
    {
        services.AddCacheNamespace(ReferenceDataCacheKeys.CountriesNamespace, p =>
        {
            p.Ttl = TimeSpan.FromHours(1);
            // Bounds how long an instance that missed a broadcast can serve an old list.
            p.LocalTtl = TimeSpan.FromMinutes(1);
        });

        services.AddSingleton<CountryRepository>();
        services.AddSingleton<ICountryCatalog, CachedCountryCatalog>();
        services.AddCacheWarmer<CountriesWarmer>();
        return services;
    }
}
