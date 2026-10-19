using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.ReferenceData.Data;
using CacheShowcase.Modules.ReferenceData.Domain;
using FastEndpoints;

namespace CacheShowcase.Modules.ReferenceData.Features.ListCountries;

/// <summary><c>GET /api/reference-data/countries</c>. Two-level cache, warmed at startup.</summary>
public sealed class ListCountriesEndpoint(ICountryCatalog countries, RequestCacheStatus cacheStatus)
    : EndpointWithoutRequest<IReadOnlyList<Country>>
{
    private static readonly string CacheControlValue = CacheControl.Public(maxAge: TimeSpan.FromMinutes(5));

    public override void Configure() => Get("/reference-data/countries");

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await countries.GetAllAsync(ct);
        cacheStatus.Record(result.Outcome);

        HttpContext.Response.Headers.CacheControl = CacheControlValue;
        await Send.OkAsync(result.Value ?? [], ct);
    }
}
