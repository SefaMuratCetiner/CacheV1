using System.Globalization;

namespace CacheShowcase.BuildingBlocks.Http.Headers;

/// <summary>Builds <c>Cache-Control</c> values, so every endpoint states its HTTP caching contract the same way.</summary>
public static class CacheControl
{
    /// <summary>Must not be stored anywhere. The default for every response that does not opt in.</summary>
    public const string NoStore = "no-store";

    /// <summary>Content-addressed responses (the URL changes when the content does): cache for a year, never revalidate.</summary>
    public const string Immutable = "public, max-age=31536000, immutable";

    /// <summary>A response identical for every caller that any cache may store.</summary>
    /// <param name="maxAge">Freshness lifetime for browsers and other private caches.</param>
    /// <param name="sharedMaxAge">Freshness lifetime for shared caches (<c>s-maxage</c>), when it differs.</param>
    /// <param name="staleWhileRevalidate">How long a stale copy may be served while a cache refreshes it.</param>
    /// <param name="staleIfError">How long a stale copy may be served when the origin fails.</param>
    public static string Public(
        TimeSpan maxAge,
        TimeSpan? sharedMaxAge = null,
        TimeSpan? staleWhileRevalidate = null,
        TimeSpan? staleIfError = null)
    {
        List<string> directives = ["public", Directive("max-age", maxAge)];
        AddIfSet(directives, "s-maxage", sharedMaxAge);
        AddIfSet(directives, "stale-while-revalidate", staleWhileRevalidate);
        AddIfSet(directives, "stale-if-error", staleIfError);
        return string.Join(", ", directives);
    }

    /// <summary>Per-user data: only the user's browser may store it.</summary>
    public static string Private(TimeSpan maxAge) => $"private, {Directive("max-age", maxAge)}";

    private static void AddIfSet(List<string> directives, string name, TimeSpan? value)
    {
        if (value is { } set)
        {
            directives.Add(Directive(name, set));
        }
    }

    private static string Directive(string name, TimeSpan value) =>
        $"{name}={((long)value.TotalSeconds).ToString(CultureInfo.InvariantCulture)}";
}
