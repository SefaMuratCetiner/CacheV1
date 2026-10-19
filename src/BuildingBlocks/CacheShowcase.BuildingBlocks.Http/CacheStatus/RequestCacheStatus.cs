using CacheShowcase.BuildingBlocks.Caching;

namespace CacheShowcase.BuildingBlocks.Http.CacheStatus;

/// <summary>
/// Collects the outcome of every cache lookup made while handling one request, so
/// <see cref="CacheStatusHeaderMiddleware"/> can report it in <c>X-Cache</c>. Registered as scoped.
/// </summary>
public sealed class RequestCacheStatus
{
    private int _hits;
    private int _misses;
    private int _stale;
    private int _bypasses;

    public void Record(CacheOutcome outcome)
    {
        switch (outcome)
        {
            case CacheOutcome.Hit:
                Interlocked.Increment(ref _hits);
                break;
            case CacheOutcome.Miss:
                Interlocked.Increment(ref _misses);
                break;
            case CacheOutcome.Stale:
                Interlocked.Increment(ref _stale);
                break;
            case CacheOutcome.Bypass:
                Interlocked.Increment(ref _bypasses);
                break;
        }
    }

    public void Record<TId, T>(CacheBatchResult<TId, T> result)
        where TId : notnull
    {
        if (result.Bypassed)
        {
            Interlocked.Increment(ref _bypasses);
            return;
        }

        Interlocked.Add(ref _hits, result.Hits);
        Interlocked.Add(ref _misses, result.Misses);
    }

    /// <summary>
    /// <c>BYPASS</c> if any lookup skipped the cache, else <c>STALE</c> if any value was stale, else <c>HIT</c>,
    /// <c>MISS</c> or <c>PARTIAL</c> (a mix); null when no lookup happened.
    /// </summary>
    public string? HeaderValue => (_hits, _misses, _stale, _bypasses) switch
    {
        (_, _, _, > 0) => "BYPASS",
        (_, _, > 0, _) => "STALE",
        (0, 0, _, _) => null,
        (_, 0, _, _) => "HIT",
        (0, _, _, _) => "MISS",
        _ => "PARTIAL",
    };
}
