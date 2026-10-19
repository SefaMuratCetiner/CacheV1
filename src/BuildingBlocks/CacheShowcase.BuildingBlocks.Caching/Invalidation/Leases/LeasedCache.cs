using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.Leases;

/// <inheritdoc cref="ILeasedCache"/>
/// <remarks>
/// The lease is a companion key (<c>{entry}:lease</c>) holding a random token, set with <c>SET NX PX</c>.
/// Storing the value checks the token and writes in one Lua script. Both keys must live on the same node
/// (fine for a single node; a cluster would need a hash tag such as <c>{entry}</c>).
/// </remarks>
internal sealed class LeasedCache(
    ValkeyStore store,
    CacheEntryCodec codec,
    CacheKeyspace keyspace,
    IOptionsMonitor<CachingOptions> options,
    CacheMetrics metrics,
    ILogger<LeasedCache> logger) : ILeasedCache
{
    private const string LeaseSuffix = "lease";

    /// <summary>Longer than any normal load; an abandoned lease frees itself after this.</summary>
    private static readonly TimeSpan LeaseTtl = TimeSpan.FromSeconds(5);

    /// <summary>How long a caller without the lease waits before re-reading, and how many times.</summary>
    private static readonly TimeSpan WaitInterval = TimeSpan.FromMilliseconds(50);
    private const int MaxWaits = 20;

    // KEYS[1] = lease, KEYS[2] = entry; ARGV[1] = token, ARGV[2] = payload, ARGV[3] = TTL in ms (0 = don't cache).
    private const string StoreIfLeaseHeldScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
          redis.call('DEL', KEYS[1])
          if tonumber(ARGV[3]) > 0 then
            redis.call('SET', KEYS[2], ARGV[2], 'PX', ARGV[3])
          end
          return 1
        end
        return 0
        """;

    public async Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var settings = options.CurrentValue;
        var storeKey = keyspace.Resolve(key);
        var leaseKey = keyspace.ResolveCompanion(key, LeaseSuffix);

        for (var attempt = 0; settings.Enabled && attempt <= MaxWaits; attempt++)
        {
            var read = await store.TryAsync("get", storeKey, db => db.StringGetAsync(storeKey));
            if (!read.Succeeded)
            {
                break;
            }

            if (codec.TryDecode<T>(read.Value, storeKey, out var cached))
            {
                metrics.RecordLookup(key.Namespace, CachePatterns.Lease, CacheOutcome.Hit);
                return new(cached, CacheOutcome.Hit);
            }

            var token = Guid.NewGuid().ToString("N");
            var lease = await store.TryAsync("lease", leaseKey, db => db.StringSetAsync(leaseKey, token, LeaseTtl, When.NotExists));
            if (!lease.Succeeded)
            {
                break;
            }

            if (lease.Value)
            {
                metrics.RecordLookup(key.Namespace, CachePatterns.Lease, CacheOutcome.Miss);
                CacheLog.Miss(logger, CachePatterns.Lease, storeKey);
                return new(await LoadAndStoreAsync(key, storeKey, leaseKey, token, factory), CacheOutcome.Miss);
            }

            // Another caller holds the lease and is loading: wait for its value rather than loading too.
            await Task.Delay(WaitInterval, cancellationToken);
        }

        // Cache disabled or unavailable, or the lease holder took too long: read the source without caching.
        metrics.RecordLookup(key.Namespace, CachePatterns.Lease, CacheOutcome.Bypass);
        return new(await factory(cancellationToken), CacheOutcome.Bypass);
    }

    public async Task InvalidateAsync(CacheKey key, CancellationToken cancellationToken = default)
    {
        var storeKey = keyspace.Resolve(key);
        RedisKey[] keys = [storeKey, keyspace.ResolveCompanion(key, LeaseSuffix)];

        if (await store.TryExecuteAsync("delete", storeKey, db => db.KeyDeleteAsync(keys)))
        {
            metrics.RecordInvalidation(key.Namespace, CachePatterns.Lease);
            CacheLog.Invalidated(logger, CachePatterns.Lease, storeKey);
        }
    }

    private async Task<T?> LoadAndStoreAsync<T>(
        CacheKey key,
        string storeKey,
        string leaseKey,
        string token,
        Func<CancellationToken, Task<T?>> factory)
        where T : class
    {
        // The lease holder's load is shared by waiting callers, so it must not be cancelled by this request.
        var value = await metrics.TimeLoadAsync(key.Namespace, () => factory(CancellationToken.None));

        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        var ttl = EntryTtl.Resolve(value is null, policy.Ttl, policy, settings.JitterRatio) ?? TimeSpan.Zero;

        var stored = await store.TryAsync("set-if-leased", storeKey, db => db.ScriptEvaluateAsync(
            StoreIfLeaseHeldScript,
            [leaseKey, storeKey],
            [token, codec.Encode(value), (long)ttl.TotalMilliseconds]));

        if (stored.Succeeded && (long)stored.Value == 0)
        {
            // Invalidated while we were loading: our value may predate the write, so it is not cached.
            CacheLog.LeaseRevoked(logger, storeKey);
        }

        return value;
    }
}
