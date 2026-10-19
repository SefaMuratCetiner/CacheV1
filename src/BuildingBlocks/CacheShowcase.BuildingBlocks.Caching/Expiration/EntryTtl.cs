using CacheShowcase.BuildingBlocks.Caching.Expiration.Jitter;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Negative;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration;

/// <summary>The final TTL for a freshly loaded entry: positive or negative TTL, then jitter. Null means "don't cache".</summary>
internal static class EntryTtl
{
    public static TimeSpan? Resolve(bool notFound, TimeSpan positiveTtl, CacheNamespacePolicy policy, double jitterRatio) =>
        NegativeCaching.SelectTtl(notFound, positiveTtl, policy) is { } ttl ? TtlJitter.Apply(ttl, jitterRatio) : null;
}
