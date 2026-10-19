using System.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CacheShowcase.BuildingBlocks.Caching.Read.Warming;

/// <summary>
/// Runs every <see cref="ICacheWarmer"/> during startup. Hosted services start before the server accepts requests,
/// so warmed data is in place for the first request. Warming is best effort: a failure is logged and the cache
/// fills lazily instead of blocking startup.
/// </summary>
internal sealed class CacheWarmupService(IEnumerable<ICacheWarmer> warmers, ILogger<CacheWarmupService> logger) : IHostedService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        foreach (var warmer in warmers)
        {
            var started = Stopwatch.GetTimestamp();
            try
            {
                await warmer.WarmAsync(timeout.Token);
                var elapsed = Stopwatch.GetElapsedTime(started);
                CacheLog.WarmerFinished(logger, warmer.Name, elapsed);
            }
#pragma warning disable CA1031 // A failed warm-up must not stop the application from starting.
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
#pragma warning restore CA1031
            {
                CacheLog.WarmerFailed(logger, exception, warmer.Name);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
