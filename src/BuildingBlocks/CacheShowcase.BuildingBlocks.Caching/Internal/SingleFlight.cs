using System.Collections.Concurrent;

namespace CacheShowcase.BuildingBlocks.Caching.Internal;

/// <summary>
/// Request coalescing (stampede protection): concurrent callers asking for the same key share one in-flight load
/// instead of each hitting the source.
/// </summary>
/// <remarks>
/// Works per process: N instances can still run N loads for one key. <see cref="Invalidation.Leases"/> shows a
/// cross-instance alternative.
/// </remarks>
internal sealed class SingleFlight
{
    private readonly ConcurrentDictionary<string, Lazy<Task<object?>>> _inFlight = new(StringComparer.Ordinal);

    public async Task<T?> RunAsync<T>(string key, Func<Task<T?>> work, CancellationToken cancellationToken)
    {
        var flight = _inFlight.GetOrAdd(key, k => new Lazy<Task<object?>>(() => ExecuteAsync(k, work)));

        // Each caller may stop waiting on its own token; the shared work keeps running for the others.
        return (T?)await flight.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<object?> ExecuteAsync<T>(string key, Func<Task<T?>> work)
    {
        try
        {
            return await work().ConfigureAwait(false);
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }
}
