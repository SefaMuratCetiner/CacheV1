namespace CacheShowcase.BuildingBlocks.Caching;

/// <summary>What a cache lookup did. Reported in the <c>X-Cache</c> response header and in metrics.</summary>
public enum CacheOutcome
{
    /// <summary>The value came from the cache.</summary>
    Hit,

    /// <summary>The value was loaded from the source (and cached, when the pattern allows it).</summary>
    Miss,

    /// <summary>An expired-but-usable value was served while a background refresh runs (soft TTL).</summary>
    Stale,

    /// <summary>The cache was skipped: disabled by the kill switch, or unavailable.</summary>
    Bypass,
}
