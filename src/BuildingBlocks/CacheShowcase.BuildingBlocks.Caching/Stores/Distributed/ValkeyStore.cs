using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;

/// <summary>
/// The single gateway to Valkey for every pattern, and the place where the fail-open policy lives:
/// a timeout or connection error is logged and counted, and the caller gets <c>Succeeded = false</c>
/// so it can fall back to the source. Valkey is never allowed to fail a request.
/// </summary>
internal sealed class ValkeyStore(IConnectionMultiplexer connection, CacheMetrics metrics, ILogger<ValkeyStore> logger)
{
    public async Task<StoreResult<T>> TryAsync<T>(string operation, string keyForLogs, Func<IDatabase, Task<T>> command)
    {
        try
        {
            return new(true, await command(connection.GetDatabase()));
        }
        catch (Exception exception) when (IsStoreFailure(exception))
        {
            metrics.RecordError(operation);
            CacheLog.OperationFailed(logger, exception, operation, keyForLogs);
            return new(false, default!);
        }
    }

    /// <summary>Runs a command whose result is not needed; returns whether it succeeded.</summary>
    public async Task<bool> TryExecuteAsync(string operation, string keyForLogs, Func<IDatabase, Task> command)
    {
        var result = await TryAsync(operation, keyForLogs, async db =>
        {
            await command(db);
            return true;
        });
        return result.Succeeded;
    }

    public ISubscriber Subscriber => connection.GetSubscriber();

    public static bool IsStoreFailure(Exception exception) =>
        exception is RedisException or TimeoutException or ObjectDisposedException;
}

internal readonly record struct StoreResult<T>(bool Succeeded, T Value);
