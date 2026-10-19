using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace CacheShowcase.BuildingBlocks.Http.Headers;

/// <summary>Validators and conditional requests (RFC 9110, section 13).</summary>
public static class ConditionalRequests
{
    /// <summary>
    /// True when <c>If-None-Match</c> already lists <paramref name="currentETag"/> (or <c>*</c>), so the endpoint can
    /// answer <c>304 Not Modified</c> with no body. Uses weak comparison, as the RFC requires for GET.
    /// </summary>
    public static bool IsNotModified(HttpRequest request, EntityTagHeaderValue currentETag) =>
        request.GetTypedHeaders().IfNoneMatch.Any(tag =>
            tag.Equals(EntityTagHeaderValue.Any) || tag.Compare(currentETag, useStrongComparison: false));

    public static void SetETag(HttpResponse response, EntityTagHeaderValue etag) =>
        response.GetTypedHeaders().ETag = etag;

    /// <summary>A strong ETag from a row version: <c>"v{version}"</c>.</summary>
    public static EntityTagHeaderValue VersionETag(long version) => new($"\"v{version}\"");

    /// <summary>
    /// Reads the version from an <c>If-Match</c> value such as <c>"v3"</c>. Returns false for anything else,
    /// which the caller should answer with <c>412 Precondition Failed</c>.
    /// </summary>
    public static bool TryParseVersionIfMatch(string ifMatch, out long version)
    {
        version = 0;
        return EntityTagHeaderValue.TryParse(ifMatch, out var tag)
            && !tag.IsWeak
            && tag.Tag.Length > 3
            && tag.Tag.Value![1] == 'v'
            && long.TryParse(tag.Tag.AsSpan(2, tag.Tag.Length - 3), out version);
    }
}
