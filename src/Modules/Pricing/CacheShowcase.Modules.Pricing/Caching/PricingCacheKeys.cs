using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.Modules.Pricing.Caching;

internal static class PricingCacheKeys
{
    /// <summary>Latest rates for one base currency (refresh-ahead, upstream-capped TTL): <c>pricing.exchange-rates:v1:{BASE}</c>.</summary>
    public const string ExchangeRatesNamespace = "pricing.exchange-rates";

    /// <summary>The list of currencies (soft/hard TTL): <c>pricing.currencies:v1:all</c>.</summary>
    public const string CurrenciesNamespace = "pricing.currencies";

    /// <summary>Metric name of the memoized tax-rate function (in process, not a Valkey namespace).</summary>
    public const string TaxRateMemoizer = "pricing.tax-rate";

    public static CacheKey ExchangeRates(string baseCurrency) => CacheKey.Create(ExchangeRatesNamespace, 1, baseCurrency);

    public static CacheKey AllCurrencies() => CacheKey.Create(CurrenciesNamespace, 1, "all");
}
