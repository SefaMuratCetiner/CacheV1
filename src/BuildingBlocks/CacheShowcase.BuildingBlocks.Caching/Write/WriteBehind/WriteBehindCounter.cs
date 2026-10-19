using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;

/// <inheritdoc cref="IWriteBehindCounter{TSink}"/>
internal sealed class WriteBehindCounter<TSink>(
    WriteBehindBuffer<TSink> buffer,
    TSink sink,
    ValkeyStore store,
    IOptionsMonitor<CachingOptions> options,
    ILogger<WriteBehindCounter<TSink>> logger) : IWriteBehindCounter<TSink>
    where TSink : IWriteBehindSink
{
    public async Task IncrementAsync(string member, long by = 1, CancellationToken cancellationToken = default)
    {
        var buffered = options.CurrentValue.Enabled
            && await store.TryExecuteAsync("hincrby", buffer.PendingKey, db => db.HashIncrementAsync(buffer.PendingKey, member, by));

        if (!buffered)
        {
            // Fail open to the source: slower, but the increment is not lost.
            CacheLog.WriteBehindFallback(logger, buffer.Name, member);
            await sink.FlushAsync(new Dictionary<string, long> { [member] = by }, cancellationToken);
        }
    }
}
