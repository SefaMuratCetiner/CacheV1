namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;

/// <summary>
/// Write-behind (write-back) for counters: an increment is written to Valkey only (one fast <c>HINCRBY</c>), and a
/// background flusher writes the accumulated totals to the source in one batch every interval.
/// Trade-off: increments still in Valkey are lost if Valkey dies before the next flush.
/// Register with <c>AddWriteBehindCounter&lt;TSink&gt;()</c>; <typeparamref name="TSink"/> also identifies the buffer.
/// </summary>
public interface IWriteBehindCounter<TSink>
    where TSink : IWriteBehindSink
{
    /// <summary>Buffers an increment. If Valkey is unavailable the increment is written straight to the sink instead.</summary>
    Task IncrementAsync(string member, long by = 1, CancellationToken cancellationToken = default);
}
