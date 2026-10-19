using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.KeyDelete;

/// <inheritdoc cref="ICacheInvalidator"/>
internal sealed class CacheInvalidator(
    ValkeyStore store,
    CacheKeyspace keyspace,
    CacheMetrics metrics,
    ILogger<CacheInvalidator> logger) : ICacheInvalidator
{
    public Task RemoveAsync(IReadOnlyCollection<CacheKey> keys, CancellationToken cancellationToken = default) =>
        RemoveAsync(keys, CachePatterns.KeyDelete, cancellationToken);

    /// <summary>Deletes the keys in one <c>DEL</c>; <paramref name="pattern"/> tags the metric.</summary>
    internal async Task RemoveAsync(IReadOnlyCollection<CacheKey> keys, string pattern, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return;
        }

        var storeKeys = keys.Select(k => (RedisKey)keyspace.Resolve(k)).ToArray();
        var keyForLogs = string.Join(", ", storeKeys);

        if (await store.TryExecuteAsync("delete", keyForLogs, db => db.KeyDeleteAsync(storeKeys)))
        {
            foreach (var key in keys)
            {
                metrics.RecordInvalidation(key.Namespace, pattern);
            }

            CacheLog.Invalidated(logger, pattern, keyForLogs);
        }
    }
}
