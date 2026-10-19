using CacheShowcase.Modules.Carts.Domain;

namespace CacheShowcase.Modules.Carts.Data;

/// <summary>What the Carts endpoints need. Implemented by <c>CachedCartStore</c>.</summary>
public interface ICartStore
{
    Task<Cart?> GetAsync(Guid cartId, CancellationToken cancellationToken);

    Task AddItemAsync(Guid cartId, int productId, int quantity, CancellationToken cancellationToken);
}
