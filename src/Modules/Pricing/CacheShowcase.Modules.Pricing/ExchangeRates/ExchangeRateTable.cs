namespace CacheShowcase.Modules.Pricing.ExchangeRates;

/// <summary>Latest rates for one base currency. The shape cached under <c>pricing.exchange-rates</c>.</summary>
public sealed record ExchangeRateTable(
    string Base,
    DateOnly AsOf,
    IReadOnlyDictionary<string, decimal> Rates,
    DateTime FetchedAt);
