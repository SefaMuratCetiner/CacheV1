using System.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;
using CacheShowcase.BuildingBlocks.Caching.Internal;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.ProbabilisticEarly;

/// <inheritdoc cref="IEarlyExpirationCache"/>
internal sealed class EarlyExpirationCache(
    ValkeyStore store,
    CacheEntryCodec codec,
    CacheKeyspace keyspace,
    SingleFlight singleFlight,
    IOptionsMonitor<CachingOptions> options,
    TimeProvider clock,
    CacheMetrics metrics,
    ILogger<EarlyExpirationCache> logger) : IEarlyExpirationCache
{
    public async Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        var beta = CacheNamespacePolicy.Require(policy.EarlyExpirationBeta, key.Namespace, nameof(policy.EarlyExpirationBeta));
        var storeKey = keyspace.Resolve(key);

        var read = settings.Enabled
            ? await store.TryAsync("get", storeKey, db => db.StringGetAsync(storeKey))
            : default;

        if (!read.Succeeded)
        {
            metrics.RecordLookup(key.Namespace, CachePatterns.EarlyExpiration, CacheOutcome.Bypass);
            return new(await factory(CancellationToken.None), CacheOutcome.Bypass);
        }

        if (codec.TryDecode<Entry<T>>(read.Value, storeKey, out var entry) && entry is not null)
        {
            var now = clock.GetUtcNow();
            if (!ShouldRecomputeEarly(now, entry, beta))
            {
                metrics.RecordLookup(key.Namespace, CachePatterns.EarlyExpiration, CacheOutcome.Hit);
                return new(entry.Value, CacheOutcome.Hit);
            }

            CacheLog.EarlyRecompute(logger, storeKey, entry.ExpiresAt - now);
        }

        metrics.RecordLookup(key.Namespace, CachePatterns.EarlyExpiration, CacheOutcome.Miss);
        var value = await singleFlight.RunAsync(storeKey, () => LoadAndStoreAsync(key, storeKey, factory), cancellationToken);
        return new(value, CacheOutcome.Miss);
    }

    private static bool ShouldRecomputeEarly<T>(DateTimeOffset now, Entry<T> entry, double beta)
    {
        // 1 - NextDouble() is in (0, 1], so the logarithm is finite and ≤ 0.
#pragma warning disable CA5394 // Not security sensitive.
        var logRandom = Math.Log(1 - Random.Shared.NextDouble());
#pragma warning restore CA5394
        var gap = TimeSpan.FromMilliseconds(entry.LoadMilliseconds * beta * -logRandom);
        return now + gap >= entry.ExpiresAt;
    }

    private async Task<T?> LoadAndStoreAsync<T>(CacheKey key, string storeKey, Func<CancellationToken, Task<T?>> factory)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);

        var started = Stopwatch.GetTimestamp();
        var value = await factory(CancellationToken.None);
        var elapsed = Stopwatch.GetElapsedTime(started);
        metrics.RecordLoad(key.Namespace, elapsed);

        // No jitter: the probabilistic recompute already spreads refreshes out.
        var ttl = value is null ? policy.NegativeTtl : policy.Ttl;
        if (ttl is { } entryTtl)
        {
            var entry = new Entry<T>(value, clock.GetUtcNow() + entryTtl, elapsed.TotalMilliseconds);
            await store.TryAsync("set", storeKey, db => db.StringSetAsync(storeKey, codec.Encode(entry), entryTtl));
        }

        return value;
    }

    /// <summary>The value, its logical expiry and how long it took to compute (Δ).</summary>
    private sealed record Entry<T>(T? Value, DateTimeOffset ExpiresAt, double LoadMilliseconds);
}
