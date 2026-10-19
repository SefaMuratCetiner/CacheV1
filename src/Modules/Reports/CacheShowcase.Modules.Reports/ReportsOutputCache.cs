namespace CacheShowcase.Modules.Reports;

internal static class ReportsOutputCache
{
    public const string CatalogOverviewPolicy = "reports.catalog-overview";

    /// <summary>Every cached response built from catalog or inventory data carries this tag.</summary>
    public const string CatalogDataTag = "reports:catalog-data";
}
