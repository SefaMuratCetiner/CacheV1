namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;

/// <summary>
/// Writes buffered counter increments to the source of truth. Implemented by the module that owns the data.
/// Must be idempotent-friendly: after a crash between "written" and "buffer deleted", a batch can be delivered twice
/// (at-least-once).
/// </summary>
public interface IWriteBehindSink
{
    Task FlushAsync(IReadOnlyDictionary<string, long> increments, CancellationToken cancellationToken);
}
