using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Jitter;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Write.UpdateOnWrite;

/// <inheritdoc cref="IVersionedCacheWriter"/>
/// <remarks>
/// The compare and the set run in one Lua script, so they are atomic in Valkey. The cached value stays plain JSON,
/// so cache-aside and read-through reads of the same key keep working.
/// </remarks>
internal sealed class VersionedCacheWriter(
    ValkeyStore store,
    CacheEntryCodec codec,
    CacheKeyspace keyspace,
    IOptionsMonitor<CachingOptions> options,
    ILogger<VersionedCacheWriter> logger) : IVersionedCacheWriter
{
    // KEYS[1] = entry; ARGV[1] = new version, ARGV[2] = payload, ARGV[3] = TTL in ms.
    // Returns 1 when stored, otherwise the version already cached.
    private const string SetIfNewerScript = """
        local current = redis.call('GET', KEYS[1])
        if current then
          local ok, decoded = pcall(cjson.decode, current)
          if ok and type(decoded) == 'table' and decoded.version ~= nil
             and tonumber(decoded.version) >= tonumber(ARGV[1]) then
            return -tonumber(decoded.version)
          end
        end
        redis.call('SET', KEYS[1], ARGV[2], 'PX', ARGV[3])
        return 1
        """;

    public async Task<VersionedWriteResult> SetIfNewerAsync<T>(CacheKey key, T value, CancellationToken cancellationToken = default)
        where T : class, IVersioned
    {
        var settings = options.CurrentValue;
        if (!settings.Enabled)
        {
            return VersionedWriteResult.Failed;
        }

        var ttl = TtlJitter.Apply(settings.GetPolicy(key.Namespace).Ttl, settings.JitterRatio);
        var storeKey = keyspace.Resolve(key);

        var result = await store.TryAsync("set-if-newer", storeKey, db => db.ScriptEvaluateAsync(
            SetIfNewerScript,
            [storeKey],
            [value.Version, codec.Encode(value), (long)ttl.TotalMilliseconds]));

        if (!result.Succeeded)
        {
            return VersionedWriteResult.Failed;
        }

        var outcome = (long)result.Value;
        if (outcome == 1)
        {
            return VersionedWriteResult.Stored;
        }

        CacheLog.VersionedWriteSkipped(logger, storeKey, -outcome);
        return VersionedWriteResult.SkippedOlder;
    }
}
