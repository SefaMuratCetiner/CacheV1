using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.KeyDelete;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Write.UpdateOnWrite;

namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteThrough;

/// <inheritdoc cref="IWriteThroughCache"/>
/// <remarks>
/// The source is written first: if it fails, the cache is untouched. If the cache write then fails, the entry is
/// deleted instead, so the cache never keeps the value from before the write.
/// </remarks>
internal sealed class WriteThroughCache(IVersionedCacheWriter writer, CacheInvalidator invalidator) : IWriteThroughCache
{
    public async Task<T?> WriteAsync<T>(CacheKey key, Func<CancellationToken, Task<T?>> writeToSource, CancellationToken cancellationToken = default)
        where T : class, IVersioned
    {
        var written = await writeToSource(cancellationToken);
        if (written is null)
        {
            return null;
        }

        if (await writer.SetIfNewerAsync(key, written, cancellationToken) == VersionedWriteResult.Failed)
        {
            await invalidator.RemoveAsync([key], CachePatterns.WriteThrough, cancellationToken);
        }

        return written;
    }
}
