using System.Globalization;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.Leases;
using CacheShowcase.BuildingBlocks.Persistence.ChangeCapture;
using CacheShowcase.Modules.Inventory.Contracts;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CacheShowcase.Modules.Inventory.Caching;

/// <summary>
/// Change capture: the <c>inventory.stock</c> trigger notifies <c>inventory_stock_changed</c> with the product id on
/// every insert or update. This listener invalidates the cached stock and publishes <see cref="StockChanged"/>, so
/// even a warehouse system writing straight to the table keeps every cache correct.
/// </summary>
internal sealed class StockChangeListener(
    NpgsqlDataSource dataSource,
    ILeasedCache cache,
    ILogger<StockChangeListener> logger) : PostgresChangeListener(dataSource, logger)
{
    protected override string Channel => "inventory_stock_changed";

    protected override async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        if (!int.TryParse(payload, NumberStyles.None, CultureInfo.InvariantCulture, out var productId))
        {
            return;
        }

        await cache.InvalidateAsync(InventoryCacheKeys.Stock(productId), cancellationToken);
        await new StockChanged(productId).PublishAsync(Mode.WaitForAll, cancellationToken);
    }
}
