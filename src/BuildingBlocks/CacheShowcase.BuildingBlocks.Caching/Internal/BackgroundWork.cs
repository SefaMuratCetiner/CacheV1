using System.Collections.Concurrent;
using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CacheShowcase.BuildingBlocks.Caching.Internal;

/// <summary>
/// Runs cache work after the request has returned: background refreshes and delayed deletes.
/// Work stops when the application shuts down, and a failure is logged, never thrown.
/// </summary>
internal sealed class BackgroundWork(IHostApplicationLifetime lifetime, ILogger<BackgroundWork> logger)
{
    private readonly ConcurrentDictionary<string, byte> _running = new(StringComparer.Ordinal);

    /// <summary>Starts <paramref name="work"/> unless work with the same key is already running.</summary>
    public void StartOnce(string key, Func<CancellationToken, Task> work)
    {
        if (_running.TryAdd(key, 0))
        {
            _ = RunAsync(key, work, () => _running.TryRemove(key, out _));
        }
    }

    /// <summary>Starts <paramref name="work"/> after <paramref name="delay"/>.</summary>
    public void StartAfter(TimeSpan delay, string keyForLogs, Func<CancellationToken, Task> work) =>
        _ = RunAsync(
            keyForLogs,
            async stopping =>
            {
                await Task.Delay(delay, stopping);
                await work(stopping);
            },
            onCompleted: null);

    private async Task RunAsync(string key, Func<CancellationToken, Task> work, Action? onCompleted)
    {
        var stopping = lifetime.ApplicationStopping;
        try
        {
            // Leave the request's execution context before doing anything.
            await Task.Yield();
            await work(stopping);
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // Shutting down.
        }
#pragma warning disable CA1031 // Background cache work must never crash the process; the failure is logged.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            CacheLog.BackgroundRefreshFailed(logger, exception, key);
        }
        finally
        {
            onCompleted?.Invoke();
        }
    }
}
