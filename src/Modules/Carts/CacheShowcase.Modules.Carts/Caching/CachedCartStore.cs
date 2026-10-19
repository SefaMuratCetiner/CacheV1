using CacheShowcase.BuildingBlocks.Caching.Expiration.Sliding;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteAround;
using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.Modules.Carts.Data;
using CacheShowcase.Modules.Carts.Domain;

namespace CacheShowcase.Modules.Carts.Caching;

/// <summary>
/// Carts are session-like: hot while the shopper is active, then abandoned. Sliding expiration fits that: each read
/// keeps an active cart cached, and an idle cart drops out of Valkey (the database still has it). Writes are
/// write-around: the database is updated and the cached cart deleted, so the next read reloads it.
/// </summary>
internal sealed class CachedCartStore(
    CartRepository repository,
    ISlidingCache cache,
    IWriteAround writeAround,
    RequestCacheStatus cacheStatus) : ICartStore
{
    public async Task<Cart?> GetAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(CartCacheKeys.Cart(cartId), ct => repository.GetAsync(cartId, ct), cancellationToken);
        cacheStatus.Record(result.Outcome);
        return result.Value;
    }

    public Task AddItemAsync(Guid cartId, int productId, int quantity, CancellationToken cancellationToken) =>
        writeAround.WriteAsync(
            async ct =>
            {
                await repository.AddItemAsync(cartId, productId, quantity, ct);
                return cartId;
            },
            id => [CartCacheKeys.Cart(id)],
            cancellationToken);
}
