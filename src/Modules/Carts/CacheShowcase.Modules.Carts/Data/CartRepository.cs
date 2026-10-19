using CacheShowcase.Modules.Carts.Domain;
using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.Carts.Data;

/// <summary>Dapper access to the <c>carts</c> schema. Knows nothing about caching.</summary>
internal sealed class CartRepository(NpgsqlDataSource dataSource)
{
    /// <returns>The cart, or null when it has no items.</returns>
    public async Task<Cart?> GetAsync(Guid cartId, CancellationToken cancellationToken)
    {
        const string sql = """
            select product_id, quantity, updated_at
            from carts.cart_items
            where cart_id = @cartId
            order by product_id
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<Row>(new CommandDefinition(sql, new { cartId }, cancellationToken: cancellationToken))).ToList();
        return rows.Count == 0
            ? null
            : new Cart(cartId, rows.Select(r => new CartItem(r.ProductId, r.Quantity)).ToList(), rows.Max(r => r.UpdatedAt));
    }

    /// <summary>Adds the quantity to the item, creating the cart and the item when needed.</summary>
    public async Task AddItemAsync(Guid cartId, int productId, int quantity, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into carts.cart_items (cart_id, product_id, quantity)
            values (@cartId, @productId, @quantity)
            on conflict (cart_id, product_id)
            do update set quantity = carts.cart_items.quantity + excluded.quantity, updated_at = now()
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { cartId, productId, quantity }, cancellationToken: cancellationToken));
    }

    private sealed record Row
    {
        public required int ProductId { get; init; }

        public required int Quantity { get; init; }

        public required DateTime UpdatedAt { get; init; }
    }
}
