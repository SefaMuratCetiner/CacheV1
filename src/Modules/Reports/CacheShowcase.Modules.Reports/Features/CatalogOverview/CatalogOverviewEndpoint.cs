using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Catalog.Contracts;
using CacheShowcase.Modules.Inventory.Contracts;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.Reports.Features.CatalogOverview;

public sealed record CatalogOverviewResponse(
    IReadOnlyList<CategoryStatistics> Categories,
    InventorySummary Inventory,
    DateTime GeneratedAt);

/// <summary>
/// <c>GET /api/reports/catalog-overview</c>. Output caching: the whole HTTP response is stored in Valkey and replayed
/// without running this handler (replays carry an <c>Age</c> header). The cached response is tagged and evicted when
/// Catalog or Inventory publish a change (event-driven, tag-based invalidation).
/// </summary>
public sealed class CatalogOverviewEndpoint(ICatalogStatistics catalog, IInventoryStatistics inventory, TimeProvider clock)
    : EndpointWithoutRequest<CatalogOverviewResponse>
{
    private static readonly string CacheControlValue = CacheControl.Public(
        maxAge: TimeSpan.FromSeconds(30),
        sharedMaxAge: TimeSpan.FromSeconds(60));

    public override void Configure()
    {
        Get("/reports/catalog-overview");
        Options(builder => builder.CacheOutput(ReportsOutputCache.CatalogOverviewPolicy));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var categories = await catalog.GetCategoryStatisticsAsync(ct);
        var stock = await inventory.GetSummaryAsync(ct);

        HttpContext.Response.Headers.CacheControl = CacheControlValue;
        await Send.OkAsync(new CatalogOverviewResponse(categories, stock, clock.GetUtcNow().UtcDateTime), ct);
    }
}
