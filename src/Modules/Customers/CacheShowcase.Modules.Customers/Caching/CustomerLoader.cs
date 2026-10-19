using CacheShowcase.BuildingBlocks.Caching.Read.ReadThrough;
using CacheShowcase.Modules.Customers.Data;
using CacheShowcase.Modules.Customers.Domain;

namespace CacheShowcase.Modules.Customers.Caching;

/// <summary>The read-through cache's loader: registered once with the cache, so no caller passes a factory.</summary>
internal sealed class CustomerLoader(CustomerRepository repository) : ICacheLoader<int, Customer>
{
    public Task<Customer?> LoadAsync(int key, CancellationToken cancellationToken) => repository.GetByIdAsync(key, cancellationToken);
}
