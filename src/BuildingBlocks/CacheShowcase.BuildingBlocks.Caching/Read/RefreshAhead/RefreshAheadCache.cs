using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;
using CacheShowcase.BuildingBlocks.Caching.Internal;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Read.RefreshAhead;

/// <inheritdoc cref="IRefreshAheadCache"/>
internal sealed class RefreshAheadCache(
    ValkeyStore store,
    CacheEntryCodec codec,
    CacheKeyspace keyspace,
    SingleFlight singleFlight,
    BackgroundWork backgroundWork,
    IOptionsMonitor<CachingOptions> options,
    TimeProvider clock,
    CacheMetrics metrics,
    ILogger<RefreshAheadCache> logger) : IRefreshAheadCache
{
    public async Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        var ratio = CacheNamespacePolicy.Require(policy.RefreshAheadRatio, key.Namespace, nameof(policy.RefreshAheadRatio));
        var storeKey = keyspace.Resolve(key);

        var read = settings.Enabled
            ? await store.TryAsync("get", storeKey, db => db.StringGetAsync(storeKey))
            : default;

        if (!read.Succeeded)
        {
            metrics.RecordLookup(key.Namespace, CachePatterns.RefreshAhead, CacheOutcome.Bypass);
            var context = new CacheEntryContext(key, policy.Ttl);
            return new(await factory(context, CancellationToken.None), CacheOutcome.Bypass);
        }

        if (codec.TryDecode<Entry<T>>(read.Value, storeKey, out var entry) && entry is not null)
        {
            var refreshAt = entry.StoredAt + (entry.Ttl * ratio);
            if (clock.GetUtcNow() >= refreshAt)
            {
                CacheLog.RefreshScheduled(logger, storeKey, "refresh-ahead");
                backgroundWork.StartOnce(storeKey, _ => LoadAndStoreAsync(key, storeKey, factory));
            }

            metrics.RecordLookup(key.Namespace, CachePatterns.RefreshAhead, CacheOutcome.Hit);
            return new(entry.Value, CacheOutcome.Hit);
        }

        metrics.RecordLookup(key.Namespace, CachePatterns.RefreshAhead, CacheOutcome.Miss);
        CacheLog.Miss(logger, CachePatterns.RefreshAhead, storeKey);
        var value = await singleFlight.RunAsync(storeKey, () => LoadAndStoreAsync(key, storeKey, factory), cancellationToken);
        return new(value, CacheOutcome.Miss);
    }

    private async Task<T?> LoadAndStoreAsync<T>(
        CacheKey key,
        string storeKey,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        var context = new CacheEntryContext(key, policy.Ttl);

        var value = await metrics.TimeLoadAsync(key.Namespace, () => factory(context, CancellationToken.None));

        if (EntryTtl.Resolve(value is null, context.Ttl, policy, settings.JitterRatio) is { } ttl)
        {
            var entry = new Entry<T>(value, clock.GetUtcNow(), ttl);
            await store.TryAsync("set", storeKey, db => db.StringSetAsync(storeKey, codec.Encode(entry), ttl));
        }

        return value;
    }

    /// <summary>The stored envelope: the value plus when it was stored and for how long, to know when to refresh.</summary>
    private sealed record Entry<T>(T? Value, DateTimeOffset StoredAt, TimeSpan Ttl);
}
