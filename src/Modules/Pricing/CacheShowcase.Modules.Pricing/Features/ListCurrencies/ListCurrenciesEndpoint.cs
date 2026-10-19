using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Pricing.Client;
using CacheShowcase.Modules.Pricing.Currencies;
using FastEndpoints;

namespace CacheShowcase.Modules.Pricing.Features.ListCurrencies;

/// <summary>
/// <c>GET /api/pricing/currencies</c>. Soft/hard TTL: <c>X-Cache: STALE</c> means the list was served past its soft
/// TTL while a background refresh ran.
/// </summary>
public sealed class ListCurrenciesEndpoint(ICurrencyCatalog currencies, RequestCacheStatus cacheStatus)
    : EndpointWithoutRequest<IReadOnlyList<Currency>>
{
    // The HTTP equivalents of the server-side soft/hard TTL.
    private static readonly string CacheControlValue = CacheControl.Public(
        maxAge: TimeSpan.FromMinutes(5),
        staleWhileRevalidate: TimeSpan.FromMinutes(5),
        staleIfError: TimeSpan.FromHours(1));

    public override void Configure() => Get("/pricing/currencies");

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var result = await currencies.GetAllAsync(ct);
            cacheStatus.Record(result.Outcome);

            HttpContext.Response.Headers.CacheControl = CacheControlValue;
            await Send.OkAsync(result.Value ?? [], ct);
        }
        catch (ProviderUnavailableException)
        {
            await Send.ResultAsync(ProviderErrors.BadGateway("The currency provider is unavailable."));
        }
    }
}
