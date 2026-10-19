using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CacheShowcase.BuildingBlocks.Caching.Diagnostics;

/// <summary>
/// OpenTelemetry-compatible instruments for every pattern. Tags: <c>cache.namespace</c>, <c>cache.pattern</c>,
/// <c>cache.outcome</c>. Exported by the host through the meter named <see cref="MeterName"/>.
/// </summary>
public sealed class CacheMetrics : IDisposable
{
    public const string MeterName = "CacheShowcase.Caching";

    private readonly Meter _meter;
    private readonly Counter<long> _lookups;
    private readonly Counter<long> _invalidations;
    private readonly Counter<long> _errors;
    private readonly Counter<long> _writeBehindFlushed;
    private readonly Histogram<double> _loadDuration;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _lookups = _meter.CreateCounter<long>("cache.lookups", description: "Cache lookups by namespace, pattern and outcome.");
        _invalidations = _meter.CreateCounter<long>("cache.invalidations", description: "Entries invalidated by namespace and pattern.");
        _errors = _meter.CreateCounter<long>("cache.errors", description: "Cache store operations that failed and were bypassed.");
        _writeBehindFlushed = _meter.CreateCounter<long>("cache.write_behind.flushed", description: "Buffered write-behind entries written to the source.");
        _loadDuration = _meter.CreateHistogram<double>("cache.load.duration", unit: "s", description: "Time spent loading values from the source on a miss.");
    }

    public void RecordLookup(string cacheNamespace, string pattern, CacheOutcome outcome, long count = 1)
    {
        if (count > 0)
        {
            _lookups.Add(
                count,
                new("cache.namespace", cacheNamespace),
                new("cache.pattern", pattern),
                new("cache.outcome", OutcomeTag(outcome)));
        }
    }

    public void RecordInvalidation(string cacheNamespace, string pattern, long count = 1) =>
        _invalidations.Add(count, new("cache.namespace", cacheNamespace), new("cache.pattern", pattern));

    public void RecordError(string operation) => _errors.Add(1, new KeyValuePair<string, object?>("cache.operation", operation));

    public void RecordWriteBehindFlush(string buffer, long entries) =>
        _writeBehindFlushed.Add(entries, new KeyValuePair<string, object?>("cache.buffer", buffer));

    public void RecordLoad(string cacheNamespace, TimeSpan elapsed) =>
        _loadDuration.Record(elapsed.TotalSeconds, new KeyValuePair<string, object?>("cache.namespace", cacheNamespace));

    /// <summary>Runs a load and records how long it took.</summary>
    public async Task<T> TimeLoadAsync<T>(string cacheNamespace, Func<Task<T>> load)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            return await load();
        }
        finally
        {
            RecordLoad(cacheNamespace, Stopwatch.GetElapsedTime(started));
        }
    }

    public void Dispose() => _meter.Dispose();

    private static string OutcomeTag(CacheOutcome outcome) => outcome switch
    {
        CacheOutcome.Hit => "hit",
        CacheOutcome.Miss => "miss",
        CacheOutcome.Stale => "stale",
        CacheOutcome.Bypass => "bypass",
        _ => "unknown",
    };
}
