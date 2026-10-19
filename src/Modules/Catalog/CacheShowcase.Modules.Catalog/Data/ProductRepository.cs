using CacheShowcase.Modules.Catalog.Domain;
using CacheShowcase.Modules.Catalog.Images;
using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.Catalog.Data;

/// <summary>Dapper access to the <c>catalog</c> schema. Knows nothing about caching.</summary>
internal sealed class ProductRepository(NpgsqlDataSource dataSource) : IProductReader, IProductWriter
{
    private const string Columns = "id, name, category, price, version, image_hash, updated_at";

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = $"select {Columns} from catalog.products where id = @id";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyDictionary<int, Product>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        // The array travels as one parameter: = any(@ids) instead of an expanded IN list.
        const string sql = $"select {Columns} from catalog.products where id = any(@ids)";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var products = await connection.QueryAsync<Product>(new CommandDefinition(sql, new { ids = ids.ToArray() }, cancellationToken: cancellationToken));
        return products.ToDictionary(p => p.Id);
    }

    public async Task<ProductIdPage> FindIdsAsync(ProductFilter filter, CancellationToken cancellationToken)
    {
        const string sql = """
            select count(*) from catalog.products where (@category::text is null or category = @category);

            select id from catalog.products
            where (@category::text is null or category = @category)
            order by id
            limit @pageSize offset @offset;
            """;

        var parameters = new { category = filter.Category, pageSize = filter.PageSize, offset = filter.Offset };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        var total = await results.ReadSingleAsync<int>();
        var ids = (await results.ReadAsync<int>()).ToList();
        return new ProductIdPage(ids, total);
    }

    public async Task<ProductImage?> GetImageAsync(string hash, CancellationToken cancellationToken)
    {
        const string sql = "select hash, content_type, content from catalog.product_images where hash = @hash";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ProductImage>(new CommandDefinition(sql, new { hash }, cancellationToken: cancellationToken));
    }

    public async Task<Product> CreateAsync(NewProduct product, CancellationToken cancellationToken)
    {
        const string sql = $"""
            insert into catalog.products (name, category, price, image_hash)
            values (@Name, @Category, @Price, @imageHash)
            returning {Columns}
            """;

        var image = ProductImageRenderer.Render(product.Name, product.Category);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await SaveImageAsync(connection, transaction, image, cancellationToken);
        var created = await connection.QuerySingleAsync<Product>(new CommandDefinition(
            sql,
            new { product.Name, product.Category, product.Price, imageHash = image.Hash },
            transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    public async Task<ProductUpdateResult> UpdateAsync(ProductUpdate update, CancellationToken cancellationToken)
    {
        const string sql = $"""
            update catalog.products
            set name = @Name, category = @Category, price = @Price, image_hash = @imageHash,
                version = version + 1, updated_at = now()
            where id = @Id and (@ExpectedVersion::int is null or version = @ExpectedVersion)
            returning {Columns}
            """;

        var image = ProductImageRenderer.Render(update.Name, update.Category);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await SaveImageAsync(connection, transaction, image, cancellationToken);
        var updated = await connection.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(
            sql,
            new { update.Id, update.Name, update.Category, update.Price, update.ExpectedVersion, imageHash = image.Hash },
            transaction,
            cancellationToken: cancellationToken));

        if (updated is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ProductUpdateResult(ProductUpdateStatus.Updated, updated);
        }

        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "select exists (select 1 from catalog.products where id = @Id)",
            new { update.Id },
            transaction,
            cancellationToken: cancellationToken));
        return new ProductUpdateResult(exists ? ProductUpdateStatus.VersionConflict : ProductUpdateStatus.NotFound);
    }

    private static Task<int> SaveImageAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, ProductImage image, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(
            """
            insert into catalog.product_images (hash, content_type, content)
            values (@Hash, @ContentType, @Content)
            on conflict (hash) do nothing
            """,
            image,
            transaction,
            cancellationToken: cancellationToken));
}
