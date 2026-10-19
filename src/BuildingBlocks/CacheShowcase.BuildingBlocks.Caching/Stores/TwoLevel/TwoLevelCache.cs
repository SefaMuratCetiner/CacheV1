using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.Broadcast;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.KeyDelete;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;
using CacheShowcase.BuildingBlocks.Caching.Stores.InMemory;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Stores.TwoLevel;

/// <inheritdoc cref="ITwoLevelCache"/>
internal sealed class TwoLevelCache(
    LocalCache l1,
    CacheAsideCache l2,
    CacheInvalidator invalidator,
    L1InvalidationBroadcast broadcast,
    IOptionsMonitor<CachingOptions> options,
    CacheMetrics metrics) : ITwoLevelCache
{
    public async Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        var localTtl = CacheNamespacePolicy.Require(policy.LocalTtl, key.Namespace, nameof(policy.LocalTtl));

        if (settings.Enabled && l1.TryGet<T>(key.Value, out var local) && local is not null)
        {
            metrics.RecordLookup(key.Namespace, CachePatterns.Local, CacheOutcome.Hit);
            return new(local, CacheOutcome.Hit);
        }

        metrics.RecordLookup(key.Namespace, CachePatterns.Local, settings.Enabled ? CacheOutcome.Miss : CacheOutcome.Bypass);

        // L1 miss: fall through to L2 (cache-aside on Valkey), which falls through to the source.
        var result = await l2.GetOrCreateAsync(key, factory, cancellationToken);
        if (settings.Enabled && result.Value is not null)
        {
            l1.Set(key.Value, result.Value, localTtl);
        }

        return result;
    }

    public async Task InvalidateAsync(CacheKey key, CancellationToken cancellationToken = default)
    {
        await invalidator.RemoveAsync([key], CachePatterns.KeyDelete, cancellationToken);
        l1.Remove(key.Value);
        await broadcast.PublishAsync(key);
    }
}
