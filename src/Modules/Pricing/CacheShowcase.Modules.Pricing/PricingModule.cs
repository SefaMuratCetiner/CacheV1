using System.Reflection;
using CacheShowcase.Modules.Pricing.Caching;
using CacheShowcase.Modules.Pricing.Client;
using CacheShowcase.Modules.Pricing.Currencies;
using CacheShowcase.Modules.Pricing.ExchangeRates;
using CacheShowcase.Modules.Pricing.Tax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace CacheShowcase.Modules.Pricing;

/// <summary>
/// Pricing: third-party data through Refit. Demonstrates refresh-ahead, upstream-derived TTL, negative caching,
/// soft/hard TTL (stale-while-revalidate and stale-if-error on the server) and memoization.
/// </summary>
public static class PricingModule
{
    public static Assembly Assembly => typeof(PricingModule).Assembly;

    public static IServiceCollection AddPricingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // TTLs are short on purpose, so each pattern can be watched in a few minutes.
        services.AddCacheNamespace(PricingCacheKeys.ExchangeRatesNamespace, p =>
        {
            p.Ttl = TimeSpan.FromMinutes(2);
            p.RefreshAheadRatio = 0.5;
            p.NegativeTtl = TimeSpan.FromMinutes(5);
        });
        services.AddCacheNamespace(PricingCacheKeys.CurrenciesNamespace, p =>
        {
            p.SoftTtl = TimeSpan.FromSeconds(30);
            p.Ttl = TimeSpan.FromHours(1);
        });

        services.AddOptions<FrankfurterOptions>()
            .Bind(configuration.GetSection(FrankfurterOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Generated at compile time by Refit's source generator.
        services.AddRefitGeneratedClient<IFrankfurterApi>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<FrankfurterOptions>>().Value;
                client.BaseAddress = options.BaseAddress;
                client.Timeout = options.Timeout;
            });

        services.AddSingleton<FrankfurterGateway>();
        services.AddSingleton<IExchangeRateProvider, ExchangeRateProvider>();
        services.AddSingleton<ICurrencyCatalog, CurrencyCatalog>();
        services.AddMemoizer<TaxRateQuery, decimal>(PricingCacheKeys.TaxRateMemoizer, TaxRules.ResolveRate, sizeLimit: 1_000);

        return services;
    }
}
