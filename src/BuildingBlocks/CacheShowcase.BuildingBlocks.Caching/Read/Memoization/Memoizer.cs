using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace CacheShowcase.BuildingBlocks.Caching.Read.Memoization;

/// <inheritdoc cref="IMemoizer{TArg,TResult}"/>
/// <remarks>
/// Two threads may compute the same argument at the same time; for a pure function that is harmless
/// (same result), so no locking is used.
/// </remarks>
internal sealed class Memoizer<TArg, TResult>(string name, Func<TArg, TResult> function, long sizeLimit, CacheMetrics metrics)
    : IMemoizer<TArg, TResult>, IDisposable
    where TArg : notnull
{
    private readonly MemoryCache _results = new(new MemoryCacheOptions { SizeLimit = sizeLimit });

    public TResult Invoke(TArg argument)
    {
        if (_results.TryGetValue(argument, out TResult? cached))
        {
            metrics.RecordLookup(name, CachePatterns.Memoization, CacheOutcome.Hit);
            return cached!;
        }

        metrics.RecordLookup(name, CachePatterns.Memoization, CacheOutcome.Miss);
        var result = function(argument);
        _results.Set(argument, result, new MemoryCacheEntryOptions { Size = 1 });
        return result;
    }

    public void Dispose() => _results.Dispose();
}
