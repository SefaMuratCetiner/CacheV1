using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.Negative;

/// <summary>
/// Negative caching: a "not found" result is cached too, with its own shorter TTL, so repeated requests for
/// missing data stop reaching the source. A factory signals "not found" by returning null; the entry is
/// stored as JSON <c>null</c> (or an envelope with a null value).
/// </summary>
internal static class NegativeCaching
{
    /// <summary>The TTL to store a loaded value with, or null when it must not be cached.</summary>
    public static TimeSpan? SelectTtl(bool notFound, TimeSpan positiveTtl, CacheNamespacePolicy policy)
    {
        var ttl = notFound ? policy.NegativeTtl : positiveTtl;
        return ttl is { } value && value > TimeSpan.Zero ? value : null;
    }
}
