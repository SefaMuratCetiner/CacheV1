using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Jitter;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;
using CacheShowcase.BuildingBlocks.Caching.Internal;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.SoftHard;

/// <inheritdoc cref="ISoftHardCache"/>
/// <remarks>Valkey expires the entry at the hard TTL; the envelope's <c>FreshUntil</c> marks the soft TTL.</remarks>
internal sealed class SoftHardCache(
    ValkeyStore store,
    CacheEntryCodec codec,
    CacheKeyspace keyspace,
    SingleFlight singleFlight,
    BackgroundWork backgroundWork,
    IOptionsMonitor<CachingOptions> options,
    TimeProvider clock,
    CacheMetrics metrics,
    ILogger<SoftHardCache> logger) : ISoftHardCache
{
    public async Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        CacheNamespacePolicy.Require(policy.SoftTtl, key.Namespace, nameof(policy.SoftTtl));
        var storeKey = keyspace.Resolve(key);

        var read = settings.Enabled
            ? await store.TryAsync("get", storeKey, db => db.StringGetAsync(storeKey))
            : default;

        if (!read.Succeeded)
        {
            metrics.RecordLookup(key.Namespace, CachePatterns.SoftHard, CacheOutcome.Bypass);
            return new(await factory(new CacheEntryContext(key, policy.Ttl), CancellationToken.None), CacheOutcome.Bypass);
        }

        if (codec.TryDecode<Entry<T>>(read.Value, storeKey, out var entry) && entry is not null)
        {
            if (clock.GetUtcNow() < entry.FreshUntil)
            {
                metrics.RecordLookup(key.Namespace, CachePatterns.SoftHard, CacheOutcome.Hit);
                return new(entry.Value, CacheOutcome.Hit);
            }

            // Stale: answer now, refresh in the background. A failed refresh leaves this entry in place (stale-if-error).
            CacheLog.RefreshScheduled(logger, storeKey, "soft TTL passed");
            backgroundWork.StartOnce(storeKey, _ => LoadAndStoreAsync(key, storeKey, factory));
            metrics.RecordLookup(key.Namespace, CachePatterns.SoftHard, CacheOutcome.Stale);
            return new(entry.Value, CacheOutcome.Stale);
        }

        metrics.RecordLookup(key.Namespace, CachePatterns.SoftHard, CacheOutcome.Miss);
        CacheLog.Miss(logger, CachePatterns.SoftHard, storeKey);
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

        // "Not found" has no stale phase: it is fresh for NegativeTtl and then gone.
        var (soft, hard) = value is null
            ? (policy.NegativeTtl, policy.NegativeTtl)
            : (policy.SoftTtl, (TimeSpan?)context.Ttl);

        if (soft is { } softTtl && hard is { } hardTtl && hardTtl > TimeSpan.Zero)
        {
            var entry = new Entry<T>(value, clock.GetUtcNow() + TtlJitter.Apply(softTtl, settings.JitterRatio));
            await store.TryAsync("set", storeKey, db => db.StringSetAsync(storeKey, codec.Encode(entry), hardTtl));
        }

        return value;
    }

    private sealed record Entry<T>(T? Value, DateTimeOffset FreshUntil);
}
