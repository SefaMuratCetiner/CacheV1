using CacheShowcase.Modules.Inventory.Data;
using FastEndpoints;

namespace CacheShowcase.Modules.Inventory.Features.GetStock;

public sealed class GetStockRequest
{
    public int ProductId { get; init; }
}

/// <summary>
/// <c>GET /api/inventory/{productId}</c>. Server-side cache with leases. No HTTP caching: stock is too volatile for
/// browsers and proxies, so the response keeps the default <c>no-store</c>.
/// </summary>
public sealed class GetStockEndpoint(IStockStore stock) : Endpoint<GetStockRequest, StockResponse>
{
    public override void Configure() => Get("/inventory/{productId:int}");

    public override async Task HandleAsync(GetStockRequest req, CancellationToken ct)
    {
        var level = await stock.GetAsync(req.ProductId, ct);
        if (level is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(StockResponse.From(level), ct);
    }
}
