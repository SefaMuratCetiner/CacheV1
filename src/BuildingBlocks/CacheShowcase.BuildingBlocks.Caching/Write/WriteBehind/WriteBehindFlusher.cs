using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;

/// <summary>
/// Moves buffered increments to the source every interval, and once more on shutdown.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>Take a short lock so only one instance flushes.</item>
/// <item>Atomically <c>RENAME</c> the pending hash to a processing hash; new increments start a fresh pending hash.</item>
/// <item>Write the processing hash to the sink, then delete it. If the sink fails, the hash stays and is retried.</item>
/// </list>
/// </remarks>
internal sealed class WriteBehindFlusher<TSink>(
    WriteBehindBuffer<TSink> buffer,
    TSink sink,
    ValkeyStore store,
    CacheMetrics metrics,
    ILogger<WriteBehindFlusher<TSink>> logger) : BackgroundService
    where TSink : IWriteBehindSink
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(buffer.FlushInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down: one final flush shrinks the window of increments that only exist in Valkey.
            using var finalFlush = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await FlushSafelyAsync(finalFlush.Token);
        }
    }

    private async Task FlushSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await FlushAsync(cancellationToken);
        }
#pragma warning disable CA1031 // The flusher must survive any failure; the buffer is retried next interval.
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
#pragma warning restore CA1031
        {
            CacheLog.WriteBehindFlushFailed(logger, exception, buffer.Name);
        }
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N");
        var locked = await store.TryAsync("lock", buffer.LockKey, db =>
            db.StringSetAsync(buffer.LockKey, token, buffer.FlushInterval * 2, When.NotExists));
        if (!locked.Succeeded || !locked.Value)
        {
            return;
        }

        try
        {
            var claimed = await store.TryAsync("rename", buffer.PendingKey, ClaimPendingAsync);
            if (!claimed.Succeeded || !claimed.Value)
            {
                return;
            }

            var entries = await store.TryAsync("hgetall", buffer.ProcessingKey, db => db.HashGetAllAsync(buffer.ProcessingKey));
            if (!entries.Succeeded)
            {
                return;
            }

            var increments = entries.Value.ToDictionary(e => e.Name.ToString(), e => (long)e.Value, StringComparer.Ordinal);
            if (increments.Count > 0)
            {
                await sink.FlushAsync(increments, cancellationToken);
            }

            await store.TryAsync("delete", buffer.ProcessingKey, db => db.KeyDeleteAsync(buffer.ProcessingKey));
            metrics.RecordWriteBehindFlush(buffer.Name, increments.Count);
            CacheLog.WriteBehindFlushed(logger, buffer.Name, increments.Count);
        }
        finally
        {
            await store.TryAsync("unlock", buffer.LockKey, db => db.KeyDeleteAsync(buffer.LockKey));
        }
    }

    /// <summary>
    /// Ensures there is a processing hash to flush: a leftover from a failed flush is retried first; otherwise the
    /// pending hash is renamed. Returns false when there is nothing to flush.
    /// </summary>
    private async Task<bool> ClaimPendingAsync(IDatabase db)
    {
        if (await db.KeyExistsAsync(buffer.ProcessingKey))
        {
            return true;
        }

        if (!await db.KeyExistsAsync(buffer.PendingKey))
        {
            return false;
        }

        await db.KeyRenameAsync(buffer.PendingKey, buffer.ProcessingKey);
        return true;
    }
}
