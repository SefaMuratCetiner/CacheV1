namespace CacheShowcase.Modules.Inventory.Contracts;

/// <summary>Read-only figures other modules may ask Inventory for.</summary>
public interface IInventoryStatistics
{
    Task<InventorySummary> GetSummaryAsync(CancellationToken cancellationToken);
}

public sealed record InventorySummary(int ProductsTracked, long TotalUnits, int OutOfStock);
