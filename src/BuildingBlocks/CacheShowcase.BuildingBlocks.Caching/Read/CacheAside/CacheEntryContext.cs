using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;

/// <summary>Passed to a loader so it can shorten its own entry's lifetime, e.g. to respect an upstream max-age.</summary>
public sealed class CacheEntryContext(CacheKey key, TimeSpan ttl)
{
    public CacheKey Key { get; } = key;

    /// <summary>The TTL the entry will be stored with. Starts at the namespace policy's TTL.</summary>
    public TimeSpan Ttl { get; private set; } = ttl;

    /// <summary>Shortens the TTL to at most <paramref name="max"/>. Zero means "don't cache this value".</summary>
    public void LimitTtl(TimeSpan max)
    {
        if (max < Ttl)
        {
            Ttl = max < TimeSpan.Zero ? TimeSpan.Zero : max;
        }
    }
}
