using System.Net.Http.Headers;

namespace CacheShowcase.BuildingBlocks.Caching.Expiration.Upstream;

/// <summary>
/// Upstream-derived TTL: a third-party response must not be cached longer than its provider allows.
/// Combine with <see cref="Read.CacheAside.CacheEntryContext.LimitTtl"/>.
/// </summary>
public static class UpstreamFreshness
{
    /// <summary>
    /// The response's remaining freshness (<c>max-age</c> minus <c>Age</c>), zero for <c>no-store</c>/<c>no-cache</c>,
    /// or null when the provider says nothing.
    /// </summary>
    public static TimeSpan? From(HttpResponseHeaders headers)
    {
        if (headers.CacheControl is { NoStore: true } or { NoCache: true })
        {
            return TimeSpan.Zero;
        }

        if (headers.CacheControl?.MaxAge is not { } maxAge)
        {
            return null;
        }

        var remaining = maxAge - (headers.Age ?? TimeSpan.Zero);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
