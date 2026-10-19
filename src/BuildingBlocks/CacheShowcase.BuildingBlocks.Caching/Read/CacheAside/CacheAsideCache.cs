using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Jitter;
using CacheShowcase.BuildingBlocks.Caching.Internal;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;

/// <inheritdoc cref="ICacheAside"/>
internal sealed class CacheAsideCache(
    ValkeyStore store,
    CacheEntryCodec codec,
    CacheKeyspace keyspace,
    SingleFlight singleFlight,
    IOptionsMonitor<CachingOptions> options,
    CacheMetrics metrics,
    ILogger<CacheAsideCache> logger) : ICacheAside
{
    public Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken = default)
        where T : class =>
        GetOrCreateAsync(key, factory, CachePatterns.CacheAside, cancellationToken);

    /// <summary>Same as the public overload; <paramref name="pattern"/> only changes the metric tag (used by read-through).</summary>
    internal async Task<CacheResult<T>> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CacheEntryContext, CancellationToken, Task<T?>> factory,
        string pattern,
        CancellationToken cancellationToken)
        where T : class
    {
        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(key.Namespace);
        var storeKey = keyspace.Resolve(key);

        var read = settings.Enabled
            ? await store.TryAsync("get", storeKey, db => db.StringGetAsync(storeKey))
            : default;

        if (!read.Succeeded)
        {
            metrics.RecordLookup(key.Namespace, pattern, CacheOutcome.Bypass);
            return new(await LoadAsync(new CacheEntryContext(key, policy.Ttl), factory), CacheOutcome.Bypass);
        }

        if (codec.TryDecode<T>(read.Value, storeKey, out var cached))
        {
            metrics.RecordLookup(key.Namespace, pattern, CacheOutcome.Hit);
            return new(cached, CacheOutcome.Hit);
        }

        metrics.RecordLookup(key.Namespace, pattern, CacheOutcome.Miss);
        CacheLog.Miss(logger, pattern, storeKey);

        var value = await singleFlight.RunAsync(
            storeKey,
            async () =>
            {
                var context = new CacheEntryContext(key, policy.Ttl);
                var loaded = await LoadAsync(context, factory);
                if (EntryTtl.Resolve(loaded is null, context.Ttl, policy, settings.JitterRatio) is { } ttl)
                {
                    await store.TryAsync("set", storeKey, db => db.StringSetAsync(storeKey, codec.Encode(loaded), ttl));
                }

                return loaded;
            },
            cancellationToken);

        return new(value, CacheOutcome.Miss);
    }

    public async Task<CacheBatchResult<TId, T>> GetOrCreateManyAsync<TId, T>(
        IReadOnlyCollection<TId> ids,
        Func<TId, CacheKey> keyFor,
        Func<IReadOnlyCollection<TId>, CancellationToken, Task<IReadOnlyDictionary<TId, T>>> factory,
        CancellationToken cancellationToken = default)
        where TId : notnull
        where T : class
    {
        if (ids.Count == 0)
        {
            return new(new Dictionary<TId, T>(), 0, 0, Bypassed: false);
        }

        var idList = ids.ToArray();
        var keys = idList.Select(keyFor).ToArray();
        var cacheNamespace = keys[0].Namespace;
        if (keys.Any(k => k.Namespace != cacheNamespace))
        {
            throw new ArgumentException("All keys in a batch must share one namespace.", nameof(keyFor));
        }

        var settings = options.CurrentValue;
        var policy = settings.GetPolicy(cacheNamespace);
        var storeKeys = Array.ConvertAll(keys, k => (RedisKey)keyspace.Resolve(k));
        var keyForLogs = $"{cacheNamespace} (batch of {storeKeys.Length})";

        var read = settings.Enabled
            ? await store.TryAsync("mget", keyForLogs, db => db.StringGetAsync(storeKeys))
            : default;

        if (!read.Succeeded)
        {
            metrics.RecordLookup(cacheNamespace, CachePatterns.CacheAside, CacheOutcome.Bypass, idList.Length);
            return new(await factory(idList, cancellationToken), 0, idList.Length, Bypassed: true);
        }

        var found = new Dictionary<TId, T>(idList.Length);
        var missing = new List<TId>();
        for (var i = 0; i < idList.Length; i++)
        {
            if (codec.TryDecode<T>(read.Value[i], storeKeys[i]!, out var value) && value is not null)
            {
                found[idList[i]] = value;
            }
            else
            {
                missing.Add(idList[i]);
            }
        }

        var hits = found.Count;
        metrics.RecordLookup(cacheNamespace, CachePatterns.CacheAside, CacheOutcome.Hit, hits);
        metrics.RecordLookup(cacheNamespace, CachePatterns.CacheAside, CacheOutcome.Miss, missing.Count);

        if (missing.Count > 0)
        {
            var loaded = await metrics.TimeLoadAsync(cacheNamespace, () => factory(missing, cancellationToken));
            foreach (var (id, value) in loaded)
            {
                found[id] = value;
            }

            await WriteManyAsync(loaded, keyFor, policy.Ttl, settings.JitterRatio, keyForLogs);
        }

        return new(found, hits, missing.Count, Bypassed: false);
    }

    // The factory gets CancellationToken.None: under single-flight its result is shared by several callers.
    private Task<T?> LoadAsync<T>(CacheEntryContext context, Func<CacheEntryContext, CancellationToken, Task<T?>> factory)
        where T : class =>
        metrics.TimeLoadAsync(context.Key.Namespace, () => factory(context, CancellationToken.None));

    private Task WriteManyAsync<TId, T>(
        IReadOnlyDictionary<TId, T> values,
        Func<TId, CacheKey> keyFor,
        TimeSpan ttl,
        double jitterRatio,
        string keyForLogs)
        where TId : notnull =>
        values.Count == 0
            ? Task.CompletedTask
            : store.TryAsync("set", keyForLogs, db =>
            {
                // Pipelined SETs, each with its own jittered TTL, sent in one round trip.
                var batch = db.CreateBatch();
                var writes = values
                    .Select(pair => batch.StringSetAsync(
                        keyspace.Resolve(keyFor(pair.Key)),
                        codec.Encode(pair.Value),
                        TtlJitter.Apply(ttl, jitterRatio)))
                    .ToArray();
                batch.Execute();
                return Task.WhenAll(writes);
            });
}
