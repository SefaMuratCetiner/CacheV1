using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Internal;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.KeyDelete;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.DoubleDelete;

/// <inheritdoc cref="IDoubleDelete"/>
internal sealed class DoubleDelete(
    CacheInvalidator invalidator,
    BackgroundWork backgroundWork,
    IOptionsMonitor<CachingOptions> options) : IDoubleDelete
{
    public async Task<T> WriteAsync<T>(
        IReadOnlyCollection<CacheKey> keys,
        Func<CancellationToken, Task<T>> writeToSource,
        CancellationToken cancellationToken = default)
    {
        await invalidator.RemoveAsync(keys, CachePatterns.DoubleDelete, cancellationToken);

        var result = await writeToSource(cancellationToken);

        // The second delete runs after the response, so the caller doesn't wait for the delay.
        backgroundWork.StartAfter(
            options.CurrentValue.DoubleDeleteDelay,
            string.Join(", ", keys),
            stopping => invalidator.RemoveAsync(keys, CachePatterns.DoubleDelete, stopping));

        return result;
    }
}
