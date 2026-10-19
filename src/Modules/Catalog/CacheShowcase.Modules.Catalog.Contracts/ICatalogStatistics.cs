namespace CacheShowcase.Modules.Catalog.Contracts;

/// <summary>Read-only figures other modules may ask Catalog for. Implemented inside the Catalog module.</summary>
public interface ICatalogStatistics
{
    Task<IReadOnlyList<CategoryStatistics>> GetCategoryStatisticsAsync(CancellationToken cancellationToken);
}

public sealed record CategoryStatistics(string Category, int ProductCount, decimal AveragePrice);
