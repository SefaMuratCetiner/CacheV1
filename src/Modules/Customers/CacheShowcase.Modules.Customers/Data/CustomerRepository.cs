using CacheShowcase.Modules.Customers.Domain;
using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.Customers.Data;

/// <summary>Dapper access to the <c>customers</c> schema. Knows nothing about caching.</summary>
internal sealed class CustomerRepository(NpgsqlDataSource dataSource)
{
    private const string Columns = "id, name, email, version, updated_at";

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = $"select {Columns} from customers.customers where id = @id";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Customer>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    /// <returns>The updated customer, or null when it does not exist.</returns>
    public async Task<Customer?> UpdateAsync(int id, string name, string email, CancellationToken cancellationToken)
    {
        const string sql = $"""
            update customers.customers
            set name = @name, email = @email, version = version + 1, updated_at = now()
            where id = @id
            returning {Columns}
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Customer>(new CommandDefinition(sql, new { id, name, email }, cancellationToken: cancellationToken));
    }
}
