using CacheShowcase.Modules.Customers.Domain;

namespace CacheShowcase.Modules.Customers.Data;

/// <summary>What the Customers endpoints need. Implemented by <c>CachedCustomerStore</c>.</summary>
public interface ICustomerStore
{
    Task<Customer?> GetAsync(int id, CancellationToken cancellationToken);

    /// <returns>The updated customer, or null when it does not exist.</returns>
    Task<Customer?> UpdateAsync(int id, string name, string email, CancellationToken cancellationToken);
}
