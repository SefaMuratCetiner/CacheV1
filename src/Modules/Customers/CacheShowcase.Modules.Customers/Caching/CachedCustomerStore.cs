using CacheShowcase.BuildingBlocks.Caching.Read.ReadThrough;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteThrough;
using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.Modules.Customers.Data;
using CacheShowcase.Modules.Customers.Domain;

namespace CacheShowcase.Modules.Customers.Caching;

/// <summary>
/// Reads through the cache and writes through it.
/// <list type="bullet">
/// <item><b>Read-through</b>: a read asks only the cache; on a miss the cache calls <see cref="CustomerLoader"/>.</item>
/// <item><b>Write-through</b>: an update writes the database, then stores the new value in the cache (only if its
/// version is newer than what is cached). The next read is a fresh hit: read-your-writes.</item>
/// </list>
/// </summary>
internal sealed class CachedCustomerStore(
    IReadThroughCache<int, Customer> cache,
    IWriteThroughCache writeThrough,
    CustomerRepository repository,
    RequestCacheStatus cacheStatus) : ICustomerStore
{
    public async Task<Customer?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var result = await cache.GetAsync(id, cancellationToken);
        cacheStatus.Record(result.Outcome);
        return result.Value;
    }

    public Task<Customer?> UpdateAsync(int id, string name, string email, CancellationToken cancellationToken) =>
        writeThrough.WriteAsync(
            CustomerCacheKeys.ById(id),
            ct => repository.UpdateAsync(id, name, email, ct),
            cancellationToken);
}
