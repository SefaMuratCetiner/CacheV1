using System.Globalization;
using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.Generations;

/// <inheritdoc cref="ICacheGenerations"/>
internal sealed class CacheGenerations(
    ValkeyStore store,
    CacheKeyspace keyspace,
    IOptionsMonitor<CachingOptions> options,
    CacheMetrics metrics,
    ILogger<CacheGenerations> logger) : ICacheGenerations
{
    public async Task<long?> GetAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        if (!options.CurrentValue.Enabled)
        {
            return null;
        }

        var storeKey = GenerationKey(cacheNamespace);
        var read = await store.TryAsync("get", storeKey, db => db.StringGetAsync(storeKey));
        if (!read.Succeeded)
        {
            return null;
        }

        return read.Value.HasValue ? long.Parse(read.Value.ToString(), CultureInfo.InvariantCulture) : 0;
    }

    public async Task AdvanceAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        var storeKey = GenerationKey(cacheNamespace);
        var advanced = await store.TryAsync("incr", storeKey, async db =>
        {
            // INCR and EXPIRE in one MULTI/EXEC, so the counter is never left without a TTL.
            var transaction = db.CreateTransaction();
            var increment = transaction.StringIncrementAsync(storeKey);
            _ = transaction.KeyExpireAsync(storeKey, options.CurrentValue.GenerationTtl);
            await transaction.ExecuteAsync();
            return await increment;
        });

        if (advanced.Succeeded)
        {
            metrics.RecordInvalidation(cacheNamespace, CachePatterns.Generation);
            CacheLog.GenerationAdvanced(logger, cacheNamespace, advanced.Value);
        }
    }

    private string GenerationKey(string cacheNamespace)
    {
        CacheKey.EnsureValidNamespace(cacheNamespace);
        return keyspace.ResolveInternal(cacheNamespace, "generation");
    }
}
