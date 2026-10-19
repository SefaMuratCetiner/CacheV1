using CacheShowcase.Modules.Inventory.Domain;

namespace CacheShowcase.Modules.Inventory.Features;

public sealed record StockResponse(int ProductId, int Quantity, DateTime UpdatedAt)
{
    public static StockResponse From(StockLevel level) => new(level.ProductId, level.Quantity, level.UpdatedAt);
}
