namespace CacheShowcase.Modules.Inventory.Domain;

/// <summary>Units in stock for one product. The shape cached under <c>inventory.stock</c>.</summary>
public sealed record StockLevel
{
    public required int ProductId { get; init; }

    public required int Quantity { get; init; }

    public required DateTime UpdatedAt { get; init; }
}
