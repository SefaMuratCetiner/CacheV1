using CacheShowcase.BuildingBlocks.Caching;
using CacheShowcase.BuildingBlocks.Caching.Expiration.SoftHard;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Upstream;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;
using CacheShowcase.Modules.Pricing.Caching;
using CacheShowcase.Modules.Pricing.Client;

namespace CacheShowcase.Modules.Pricing.Currencies;

public interface ICurrencyCatalog
{
    /// <exception cref="ProviderUnavailableException">The provider failed and nothing is cached.</exception>
    Task<CacheResult<IReadOnlyList<Currency>>> GetAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The currency list behind a soft/hard TTL cache: after the soft TTL the list is served stale while one background
/// refresh runs, and if the provider is down the stale list keeps being served until the hard TTL
/// (server-side stale-while-revalidate and stale-if-error).
/// </summary>
internal sealed class CurrencyCatalog(FrankfurterGateway frankfurter, ISoftHardCache cache) : ICurrencyCatalog
{
    public Task<CacheResult<IReadOnlyList<Currency>>> GetAllAsync(CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(PricingCacheKeys.AllCurrencies(), (entry, _) => FetchAsync(entry), cancellationToken);

    private async Task<IReadOnlyList<Currency>?> FetchAsync(CacheEntryContext entry)
    {
        var response = await frankfurter.CallAsync(async api =>
        {
            try
            {
                return await api.GetCurrenciesAsync(CancellationToken.None);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                throw new ProviderUnavailableException("The currency provider is unreachable.", exception);
            }
        });

        if (!response.IsSuccessStatusCode || response.Content is not { Count: > 0 } currencies)
        {
            throw new ProviderUnavailableException(
                $"The currency provider returned {(int?)response.StatusCode}.",
                (Exception?)response.Error ?? new HttpRequestException(response.ReasonPhrase));
        }

        if (UpstreamFreshness.From(response.Headers) is { } freshness)
        {
            entry.LimitTtl(freshness);
        }

        return currencies.Select(c => new Currency(c.IsoCode, c.Name, c.Symbol)).ToList();
    }
}
