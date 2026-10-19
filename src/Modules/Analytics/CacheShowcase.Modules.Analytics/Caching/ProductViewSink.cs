using System.Globalization;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;
using CacheShowcase.Modules.Analytics.Data;

namespace CacheShowcase.Modules.Analytics.Caching;

/// <summary>Where the write-behind buffer of product views is flushed: one batched upsert per interval.</summary>
internal sealed class ProductViewSink(ProductViewRepository repository) : IWriteBehindSink
{
    public Task FlushAsync(IReadOnlyDictionary<string, long> increments, CancellationToken cancellationToken)
    {
        var views = increments.ToDictionary(
            pair => int.Parse(pair.Key, NumberStyles.None, CultureInfo.InvariantCulture),
            pair => pair.Value);

        return repository.AddViewsAsync(views, cancellationToken);
    }
}
