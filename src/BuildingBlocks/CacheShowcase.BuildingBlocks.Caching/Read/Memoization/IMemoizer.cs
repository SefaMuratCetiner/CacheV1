namespace CacheShowcase.BuildingBlocks.Caching.Read.Memoization;

/// <summary>
/// Memoization: remembers a pure function's result per argument, in process. No TTL and no invalidation are needed,
/// because a pure function always returns the same result for the same argument; the size limit bounds memory.
/// Register with <c>AddMemoizer</c>.
/// </summary>
public interface IMemoizer<in TArg, out TResult>
    where TArg : notnull
{
    TResult Invoke(TArg argument);
}
