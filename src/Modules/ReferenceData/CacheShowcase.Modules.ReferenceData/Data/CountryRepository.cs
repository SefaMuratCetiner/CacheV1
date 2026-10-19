using CacheShowcase.Modules.ReferenceData.Domain;
using Dapper;
using Npgsql;

namespace CacheShowcase.Modules.ReferenceData.Data;

/// <summary>Dapper access to the <c>reference_data</c> schema. Knows nothing about caching.</summary>
internal sealed class CountryRepository(NpgsqlDataSource dataSource)
{
    public async Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = "select code, name, currency from reference_data.countries order by code";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<Country>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    /// <returns>The updated country, or null when the code is unknown.</returns>
    public async Task<Country?> UpdateAsync(string code, string name, string currency, CancellationToken cancellationToken)
    {
        const string sql = """
            update reference_data.countries set name = @name, currency = @currency
            where code = @code
            returning code, name, currency
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Country>(new CommandDefinition(sql, new { code, name, currency }, cancellationToken: cancellationToken));
    }
}
