using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;

namespace CacheShowcase.BuildingBlocks.Caching.Read.RefreshAhead;

/// <summary>
/// Refresh-ahead: once an entry has lived <c>RefreshAheadRatio</c> of its TTL, the next read still gets the cached
/// value immediately, and a background task reloads it. Hot keys therefore never expire under traffic and no caller
/// waits for the reload. Requires <c>RefreshAheadRatio</c> on the namespace.
/// </summary>
public interface IRefreshAheadCache
{
    /// <param name="factory">Loads the value. Also used for background refreshes, so it must not depend on the request.</param>
    Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class;
}
