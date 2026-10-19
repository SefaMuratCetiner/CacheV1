using CacheShowcase.Modules.Inventory.Domain;

namespace CacheShowcase.Modules.Inventory.Data;

/// <summary>What the Inventory endpoints need. Implemented by <c>CachedStockStore</c>.</summary>
public interface IStockStore
{
    Task<StockLevel?> GetAsync(int productId, CancellationToken cancellationToken);

    Task<StockAdjustmentResult> AdjustAsync(int productId, int delta, CancellationToken cancellationToken);
}
