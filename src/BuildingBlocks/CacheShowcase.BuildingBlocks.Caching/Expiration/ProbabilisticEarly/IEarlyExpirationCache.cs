using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.ProbabilisticEarly;

/// <summary>
/// Probabilistic early expiration (XFetch, Vattani et al. 2015). Each read may decide to recompute before the entry
/// expires; the chance rises as expiry approaches and with how slow the value is to compute:
/// <code>recompute when  now − Δ · β · ln(rand()) ≥ expiry</code>
/// (Δ = last load time, β = <c>EarlyExpirationBeta</c>). One early reader refreshes the entry while everyone else
/// still gets hits, so a hot key never expires for all callers at once, with no locks or coordination.
/// Requires <c>EarlyExpirationBeta</c> on the namespace.
/// </summary>
public interface IEarlyExpirationCache
{
    Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class;
}
