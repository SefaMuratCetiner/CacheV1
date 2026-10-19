using CacheShowcase.Modules.Inventory.Contracts;
using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.Inventory.Statistics;

/// <summary>Implements the public <see cref="IInventoryStatistics"/> contract.</summary>
internal sealed class InventoryStatistics(NpgsqlDataSource dataSource) : IInventoryStatistics
{
    public async Task<InventorySummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select count(*)::int as products_tracked,
                   coalesce(sum(quantity), 0)::bigint as total_units,
                   count(*) filter (where quantity = 0)::int as out_of_stock
            from inventory.stock
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<Row>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return new InventorySummary(row.ProductsTracked, row.TotalUnits, row.OutOfStock);
    }

    private sealed record Row
    {
        public required int ProductsTracked { get; init; }

        public required long TotalUnits { get; init; }

        public required int OutOfStock { get; init; }
    }
}
