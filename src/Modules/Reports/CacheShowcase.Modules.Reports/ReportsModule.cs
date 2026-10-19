using System.Reflection;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CacheShowcase.Modules.Reports;

/// <summary>
/// Reports: aggregates data owned by other modules. Demonstrates output caching, tag-based eviction and event-driven
/// invalidation across module boundaries.
/// </summary>
public static class ReportsModule
{
    public static Assembly Assembly => typeof(ReportsModule).Assembly;

    public static IServiceCollection AddReportsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ReportsOptions>().Bind(configuration.GetSection(ReportsOptions.SectionName));

        // The module owns its output cache policy; the TTL comes from configuration.
        services.AddOptions<OutputCacheOptions>()
            .Configure<IOptions<ReportsOptions>>((outputCache, reports) =>
                outputCache.AddPolicy(ReportsOutputCache.CatalogOverviewPolicy, policy => policy
                    .Expire(reports.Value.CatalogOverviewTtl)
                    // The response depends on no query parameter, so utm_* and other noise can't multiply entries.
                    .SetVaryByQuery([])
                    .Tag(ReportsOutputCache.CatalogDataTag)));

        return services;
    }
}
