using CacheShowcase.Modules.Inventory.Domain;
using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.Inventory.Data;

public enum StockAdjustmentStatus
{
    Adjusted,
    NotFound,
    InsufficientStock,
}

public sealed record StockAdjustmentResult(StockAdjustmentStatus Status, StockLevel? Level = null);

/// <summary>
/// Dapper access to the <c>inventory</c> schema. A trigger on <c>inventory.stock</c> sends a notification for every
/// change (see <c>db/init/04_inventory.sql</c>). Knows nothing about caching.
/// </summary>
internal sealed class StockRepository(NpgsqlDataSource dataSource)
{
    private const string Columns = "product_id, quantity, updated_at";

    public async Task<StockLevel?> GetAsync(int productId, CancellationToken cancellationToken)
    {
        const string sql = $"select {Columns} from inventory.stock where product_id = @productId";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<StockLevel>(new CommandDefinition(sql, new { productId }, cancellationToken: cancellationToken));
    }

    public async Task<StockAdjustmentResult> AdjustAsync(int productId, int delta, CancellationToken cancellationToken)
    {
        // One atomic statement: no read-modify-write race, and stock can't go negative.
        const string sql = $"""
            update inventory.stock
            set quantity = quantity + @delta, updated_at = now()
            where product_id = @productId and quantity + @delta >= 0
            returning {Columns}
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var level = await connection.QuerySingleOrDefaultAsync<StockLevel>(new CommandDefinition(sql, new { productId, delta }, cancellationToken: cancellationToken));
        if (level is not null)
        {
            return new StockAdjustmentResult(StockAdjustmentStatus.Adjusted, level);
        }

        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "select exists (select 1 from inventory.stock where product_id = @productId)",
            new { productId },
            cancellationToken: cancellationToken));
        return new StockAdjustmentResult(exists ? StockAdjustmentStatus.InsufficientStock : StockAdjustmentStatus.NotFound);
    }
}
