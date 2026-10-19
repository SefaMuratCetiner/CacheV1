using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.KeyDelete;
using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteAround;

/// <inheritdoc cref="IWriteAround"/>
/// <remarks>Order matters: source first, then invalidate. A failed invalidation is logged and the TTL bounds the staleness.</remarks>
internal sealed class WriteAround(CacheInvalidator invalidator) : IWriteAround
{
    public async Task<T> WriteAsync<T>(
        Func<CancellationToken, Task<T>> writeToSource,
        Func<T, IReadOnlyCollection<CacheKey>> affectedKeys,
        CancellationToken cancellationToken = default)
    {
        var result = await writeToSource(cancellationToken);
        await invalidator.RemoveAsync(affectedKeys(result), CachePatterns.KeyDelete, cancellationToken);
        return result;
    }
}
