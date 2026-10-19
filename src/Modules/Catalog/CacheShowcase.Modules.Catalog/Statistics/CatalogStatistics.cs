using CacheShowcase.Modules.Catalog.Contracts;
using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.Catalog.Statistics;

/// <summary>
/// Implements the public <see cref="ICatalogStatistics"/> contract. Not cached here: the consumer (Reports) caches
/// the whole response it builds from this.
/// </summary>
internal sealed class CatalogStatistics(NpgsqlDataSource dataSource) : ICatalogStatistics
{
    public async Task<IReadOnlyList<CategoryStatistics>> GetCategoryStatisticsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select category, count(*)::int as product_count, round(avg(price), 2) as average_price
            from catalog.products
            group by category
            order by category
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<Row>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(r => new CategoryStatistics(r.Category, r.ProductCount, r.AveragePrice)).ToList();
    }

    private sealed record Row
    {
        public required string Category { get; init; }

        public required int ProductCount { get; init; }

        public required decimal AveragePrice { get; init; }
    }
}
