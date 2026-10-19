using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.Analytics.Data;

public sealed record TrendingProduct(int ProductId, long Views);

/// <summary>Dapper access to the <c>analytics</c> schema. Knows nothing about caching.</summary>
internal sealed class ProductViewRepository(NpgsqlDataSource dataSource)
{
    /// <summary>Adds a batch of view counts in one statement.</summary>
    public async Task AddViewsAsync(IReadOnlyDictionary<int, long> views, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into analytics.product_views (product_id, views)
            select * from unnest(@productIds, @counts)
            on conflict (product_id)
            do update set views = analytics.product_views.views + excluded.views, updated_at = now()
            """;

        var parameters = new { productIds = views.Keys.ToArray(), counts = views.Values.ToArray() };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<TrendingProduct>> GetTrendingAsync(int limit, CancellationToken cancellationToken)
    {
        // pg_sleep stands in for an expensive aggregation, so the probabilistic early expiration is observable:
        // the slower a value is to compute, the earlier XFetch starts recomputing it.
        const string sql = """
            select product_id, views
            from analytics.product_views, pg_sleep(0.25)
            order by views desc, product_id
            limit @limit
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<Row>(new CommandDefinition(sql, new { limit }, cancellationToken: cancellationToken));
        return rows.Select(r => new TrendingProduct(r.ProductId, r.Views)).ToList();
    }

    private sealed record Row
    {
        public required int ProductId { get; init; }

        public required long Views { get; init; }
    }
}
