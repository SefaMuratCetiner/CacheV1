namespace CacheShowcase.Modules.Reports;

public sealed class ReportsOptions
{
    public const string SectionName = "Reports";

    /// <summary>How long the output cache keeps the catalog overview, unless an event evicts it first.</summary>
    public TimeSpan CatalogOverviewTtl { get; set; } = TimeSpan.FromMinutes(1);
}
