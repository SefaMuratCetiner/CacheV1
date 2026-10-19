namespace CacheShowcase.BuildingBlocks.Caching;

/// <summary>A value and how it was obtained. <see cref="Value"/> is null for "not found".</summary>
public readonly record struct CacheResult<T>(T? Value, CacheOutcome Outcome)
    where T : class;

/// <summary>The result of a batch lookup: every value found (cached or loaded) plus hit/miss counts.</summary>
public sealed record CacheBatchResult<TId, T>(IReadOnlyDictionary<TId, T> Values, int Hits, int Misses, bool Bypassed)
    where TId : notnull;
