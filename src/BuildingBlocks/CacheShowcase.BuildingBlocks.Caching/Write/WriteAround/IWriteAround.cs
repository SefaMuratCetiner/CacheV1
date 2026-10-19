using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteAround;

/// <summary>
/// Write-around with invalidate-on-write: the write goes only to the source; afterwards the affected entries are
/// deleted (not updated), and the next read reloads them. Deleting is idempotent and safe to retry, which makes it
/// the safest default.
/// </summary>
public interface IWriteAround
{
    /// <param name="writeToSource">Writes the source of truth.</param>
    /// <param name="affectedKeys">The entries the write made stale, computed from the write's result.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<T> WriteAsync<T>(
        Func<CancellationToken, Task<T>> writeToSource,
        Func<T, IReadOnlyCollection<CacheKey>> affectedKeys,
        CancellationToken cancellationToken = default);
}
