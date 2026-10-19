using Dapper;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers one pooled <see cref="Npgsql.NpgsqlDataSource"/>. Every module uses its own schema
    /// (<c>catalog</c>, <c>inventory</c>, …) and never queries another module's tables.
    /// </summary>
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is missing.");

        services.AddNpgsqlDataSource(connectionString);

        // Map snake_case columns (image_hash) to PascalCase properties (ImageHash).
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        return services;
    }
}
