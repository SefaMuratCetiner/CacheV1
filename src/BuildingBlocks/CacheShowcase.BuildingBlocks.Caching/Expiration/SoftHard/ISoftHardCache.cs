using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.SoftHard;

/// <summary>
/// Soft and hard TTL, the server-side version of <c>stale-while-revalidate</c> and <c>stale-if-error</c>:
/// <list type="bullet">
/// <item>Before <c>SoftTtl</c>: fresh, served as a hit.</item>
/// <item>Between <c>SoftTtl</c> and <c>Ttl</c>: stale but usable. Served immediately (<see cref="CacheOutcome.Stale"/>)
/// while one background refresh runs. If that refresh fails, the stale value keeps being served.</item>
/// <item>After <c>Ttl</c>: gone; the next caller loads synchronously.</item>
/// </list>
/// Requires <c>SoftTtl</c> on the namespace.
/// </summary>
public interface ISoftHardCache
{
    Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class;
}
