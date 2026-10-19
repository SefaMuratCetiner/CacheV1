using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Analytics.Data;
using FastEndpoints;
using FluentValidation;

namespace CacheShowcase.Modules.Analytics.Features.GetTrendingProducts;

public sealed class GetTrendingProductsRequest
{
    public int Limit { get; init; } = 10;
}

public sealed class GetTrendingProductsValidator : Validator<GetTrendingProductsRequest>
{
    public GetTrendingProductsValidator() => RuleFor(x => x.Limit).InclusiveBetween(1, 50);
}

/// <summary>
/// <c>GET /api/analytics/trending?limit=10</c>. TTL-only invalidation (a few seconds of staleness is fine for a
/// trending list) with probabilistic early expiration, so the slow query is recomputed by one early caller before the
/// entry expires for everyone.
/// </summary>
public sealed class GetTrendingProductsEndpoint(IProductViewStore views)
    : Endpoint<GetTrendingProductsRequest, IReadOnlyList<TrendingProduct>>
{
    private static readonly string CacheControlValue = CacheControl.Public(maxAge: TimeSpan.FromSeconds(10));

    public override void Configure() => Get("/analytics/trending");

    public override async Task HandleAsync(GetTrendingProductsRequest req, CancellationToken ct)
    {
        var trending = await views.GetTrendingAsync(req.Limit, ct);
        HttpContext.Response.Headers.CacheControl = CacheControlValue;
        await Send.OkAsync(trending, ct);
    }
}
