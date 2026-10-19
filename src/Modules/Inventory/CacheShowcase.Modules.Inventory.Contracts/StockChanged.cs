using FastEndpoints;

namespace CacheShowcase.Modules.Inventory.Contracts;

/// <summary>
/// Published for every stock change the database reports, whoever made it (this API, another system, manual SQL).
/// </summary>
public sealed record StockChanged(int ProductId) : IEvent;
