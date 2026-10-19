using System.Net;
using CacheShowcase.BuildingBlocks.Caching;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Upstream;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;
using CacheShowcase.BuildingBlocks.Caching.Read.RefreshAhead;
using CacheShowcase.Modules.Pricing.Caching;
using CacheShowcase.Modules.Pricing.Client;

namespace CacheShowcase.Modules.Pricing.ExchangeRates;

public interface IExchangeRateProvider
{
    /// <summary>Latest rates for an uppercase ISO 4217 code; <c>Value</c> is null for an unknown currency.</summary>
    /// <exception cref="ProviderUnavailableException">The provider failed and nothing is cached.</exception>
    Task<CacheResult<ExchangeRateTable>> GetLatestAsync(string baseCurrency, CancellationToken cancellationToken);
}

/// <summary>
/// Third-party rates behind a refresh-ahead cache: hot currencies are reloaded in the background before they expire,
/// so callers never wait on the provider after the first load. Each entry's TTL is also capped by the provider's own
/// freshness (<c>max-age</c> minus <c>Age</c>), and an unknown currency is negatively cached.
/// </summary>
internal sealed class ExchangeRateProvider(FrankfurterGateway frankfurter, IRefreshAheadCache cache, TimeProvider clock)
    : IExchangeRateProvider
{
    public Task<CacheResult<ExchangeRateTable>> GetLatestAsync(string baseCurrency, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            PricingCacheKeys.ExchangeRates(baseCurrency),
            (entry, _) => FetchAsync(baseCurrency, entry),
            cancellationToken);

    private async Task<ExchangeRateTable?> FetchAsync(string baseCurrency, CacheEntryContext entry)
    {
        var response = await frankfurter.CallAsync(async api =>
        {
            try
            {
                return await api.GetLatestRatesAsync(baseCurrency, CancellationToken.None);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                throw new ProviderUnavailableException("The exchange rate provider is unreachable.", exception);
            }
        });

        // Frankfurter answers 422 for an unknown currency: a real "not found", negatively cached.
        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode || response.Content is not { Count: > 0 } rates)
        {
            throw new ProviderUnavailableException(
                $"The exchange rate provider returned {(int?)response.StatusCode}.",
                (Exception?)response.Error ?? new HttpRequestException(response.ReasonPhrase));
        }

        // Upstream-derived TTL: never keep the rates longer than the provider allows.
        if (UpstreamFreshness.From(response.Headers) is { } freshness)
        {
            entry.LimitTtl(freshness);
        }

        return new ExchangeRateTable(
            baseCurrency,
            rates.Max(r => r.Date),
            rates.ToDictionary(r => r.Quote, r => r.Rate, StringComparer.Ordinal),
            clock.GetUtcNow().UtcDateTime);
    }
}
