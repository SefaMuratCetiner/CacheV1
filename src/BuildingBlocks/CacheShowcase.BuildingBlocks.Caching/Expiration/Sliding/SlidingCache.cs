using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;
using CacheShowcase.BuildingBlocks.Caching.Internal;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.Sliding;

/// <inheritdoc cref="ISlidingCache"/>
/// <remarks>
/// <c>GETEX key PX sliding</c> reads the entry and resets its idle timer in one command. The envelope's
/// <c>CreatedAt</c> enforces the absolute cap.
/// </remarks>
internal sealed class SlidingCache(
    ValkeyStore store,
    CacheEntryCodec codec,
    CacheKeyspace keyspace,
    SingleFlight singleFlight,
    IOptionsMonitor<CachingOptions> options,
    TimeProvider clock,
    CacheMetrics metrics,
    ILogger<SlidingCache> logger) : ISlidingCache
{
    public async Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        var sliding = CacheNamespacePolicy.Require(policy.SlidingTtl, key.Namespace, nameof(policy.SlidingTtl));
        var storeKey = keyspace.Resolve(key);

        var read = settings.Enabled
            ? await store.TryAsync("getex", storeKey, db => db.StringGetSetExpiryAsync(storeKey, sliding))
            : default;

        if (!read.Succeeded)
        {
            metrics.RecordLookup(key.Namespace, CachePatterns.Sliding, CacheOutcome.Bypass);
            return new(await factory(CancellationToken.None), CacheOutcome.Bypass);
        }

        if (codec.TryDecode<Entry<T>>(read.Value, storeKey, out var entry)
            && entry?.Value is not null
            && clock.GetUtcNow() < entry.CreatedAt + policy.Ttl)
        {
            metrics.RecordLookup(key.Namespace, CachePatterns.Sliding, CacheOutcome.Hit);
            return new(entry.Value, CacheOutcome.Hit);
        }

        metrics.RecordLookup(key.Namespace, CachePatterns.Sliding, CacheOutcome.Miss);
        CacheLog.Miss(logger, CachePatterns.Sliding, storeKey);

        var value = await singleFlight.RunAsync(
            storeKey,
            async () =>
            {
                var loaded = await metrics.TimeLoadAsync(key.Namespace, () => factory(CancellationToken.None));
                if (loaded is not null)
                {
                    var created = new Entry<T>(loaded, clock.GetUtcNow());
                    await store.TryAsync("set", storeKey, db => db.StringSetAsync(storeKey, codec.Encode(created), sliding));
                }

                return loaded;
            },
            cancellationToken);

        return new(value, CacheOutcome.Miss);
    }

    private sealed record Entry<T>(T? Value, DateTimeOffset CreatedAt);
}
