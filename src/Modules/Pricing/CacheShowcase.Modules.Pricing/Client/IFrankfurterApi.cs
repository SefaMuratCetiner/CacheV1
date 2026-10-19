using System.Text.Json.Serialization;
using Refit;

namespace CacheShowcase.Modules.Pricing.Client;

/// <summary>
/// Frankfurter (https://frankfurter.dev): free, keyless exchange rates from central banks. Calls return
/// <see cref="IApiResponse{T}"/> instead of throwing, so the caller can read the upstream <c>Cache-Control</c> and
/// tell "unknown currency" (422) apart from an outage.
/// </summary>
public interface IFrankfurterApi
{
    [Get("/v2/rates")]
    Task<IApiResponse<IReadOnlyList<FrankfurterRate>>> GetLatestRatesAsync(
        [AliasAs("base")] string baseCurrency,
        CancellationToken cancellationToken);

    [Get("/v2/currencies")]
    Task<IApiResponse<IReadOnlyList<FrankfurterCurrency>>> GetCurrenciesAsync(CancellationToken cancellationToken);
}

public sealed record FrankfurterRate(DateOnly Date, string Base, string Quote, decimal Rate);

public sealed record FrankfurterCurrency(
    [property: JsonPropertyName("iso_code")] string IsoCode,
    string Name,
    string? Symbol);
