using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Pricing.Client;
using CacheShowcase.Modules.Pricing.ExchangeRates;
using FastEndpoints;
using FluentValidation;

namespace CacheShowcase.Modules.Pricing.Features.GetExchangeRates;

public sealed class GetExchangeRatesRequest
{
    public string Base { get; init; } = string.Empty;
}

public sealed record ExchangeRatesResponse(string Base, DateOnly AsOf, IReadOnlyDictionary<string, decimal> Rates, DateTime FetchedAt);

public sealed class GetExchangeRatesValidator : Validator<GetExchangeRatesRequest>
{
    public GetExchangeRatesValidator() =>
        RuleFor(x => x.Base).Matches("^[A-Za-z]{3}$").WithMessage("Base must be a three-letter ISO 4217 code.");
}

/// <summary>
/// <c>GET /api/pricing/exchange-rates/{base}</c>. Refresh-ahead with an upstream-capped TTL. The code is uppercased
/// first, so <c>usd</c> and <c>USD</c> share one entry.
/// </summary>
public sealed class GetExchangeRatesEndpoint(IExchangeRateProvider rates, RequestCacheStatus cacheStatus)
    : Endpoint<GetExchangeRatesRequest, ExchangeRatesResponse>
{
    private static readonly string FoundCacheControl = CacheControl.Public(
        maxAge: TimeSpan.FromMinutes(1),
        staleIfError: TimeSpan.FromHours(1));

    private static readonly string NotFoundCacheControl = CacheControl.Public(maxAge: TimeSpan.FromMinutes(1));

    public override void Configure() => Get("/pricing/exchange-rates/{base}");

    public override async Task HandleAsync(GetExchangeRatesRequest req, CancellationToken ct)
    {
        try
        {
            var result = await rates.GetLatestAsync(req.Base.ToUpperInvariant(), ct);
            cacheStatus.Record(result.Outcome);

            if (result.Value is not { } table)
            {
                HttpContext.Response.Headers.CacheControl = NotFoundCacheControl;
                await Send.NotFoundAsync(ct);
                return;
            }

            HttpContext.Response.Headers.CacheControl = FoundCacheControl;
            await Send.OkAsync(new ExchangeRatesResponse(table.Base, table.AsOf, table.Rates, table.FetchedAt), ct);
        }
        catch (ProviderUnavailableException)
        {
            await Send.ResultAsync(ProviderErrors.BadGateway("The exchange rate provider is unavailable."));
        }
    }
}
