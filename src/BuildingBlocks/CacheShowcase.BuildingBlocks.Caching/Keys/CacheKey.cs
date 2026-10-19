using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CacheShowcase.BuildingBlocks.Caching.Keys;

/// <summary>
/// A structured cache key: <c>{namespace}:v{schemaVersion}:{part}:{part}…</c>, e.g. <c>catalog.product:v1:42</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><b>Namespace</b> (<c>module.name</c>): selects the TTL policy and tags metrics.
/// The module prefix keeps each module's keys separate.</item>
/// <item><b>Schema version</b>: bump it when the cached shape changes, so old entries are never read again
/// (schema-versioning invalidation).</item>
/// <item><b>Parts</b>: long or unsafe parts are hashed, so user input can't break the format or bloat keys.
/// Normalizing inputs (case, defaults) is the caller's job, because only the caller knows what is equivalent.</item>
/// </list>
/// <see cref="CacheKeyspace"/> adds the <c>{app}:{environment}:</c> prefix when the key is stored.
/// </remarks>
public readonly partial record struct CacheKey
{
    private const char Separator = ':';
    private const int MaxPartLength = 64;

    private CacheKey(string @namespace, string value)
    {
        Namespace = @namespace;
        Value = value;
    }

    /// <summary>The key's namespace, e.g. <c>catalog.product</c>.</summary>
    public string Namespace { get; }

    /// <summary>The key without the application/environment prefix.</summary>
    public string Value { get; }

    /// <exception cref="ArgumentException">The namespace is not <c>module.name</c> in lowercase kebab-case.</exception>
    public static CacheKey Create(string cacheNamespace, int schemaVersion, params ReadOnlySpan<object?> parts)
    {
        EnsureValidNamespace(cacheNamespace);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(schemaVersion);

        var builder = new StringBuilder(cacheNamespace)
            .Append(Separator)
            .Append('v')
            .Append(schemaVersion.ToString(CultureInfo.InvariantCulture));

        foreach (var part in parts)
        {
            builder.Append(Separator).Append(FormatPart(part));
        }

        return new CacheKey(cacheNamespace, builder.ToString());
    }

    /// <exception cref="ArgumentException">The namespace is not <c>module.name</c> in lowercase kebab-case.</exception>
    public static void EnsureValidNamespace(string cacheNamespace)
    {
        if (!NamespacePattern().IsMatch(cacheNamespace))
        {
            throw new ArgumentException(
                $"Cache namespace '{cacheNamespace}' must look like 'module.name' (lowercase kebab-case).",
                nameof(cacheNamespace));
        }
    }

    public override string ToString() => Value;

    private static string FormatPart(object? part)
    {
        var text = part switch
        {
            null => "_",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => part.ToString() ?? "_",
        };

        return text.Length is 0 or > MaxPartLength || !SafePartPattern().IsMatch(text) ? Hash(text) : text;
    }

    private static string Hash(string text)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(text), hash);
        return "h-" + Convert.ToHexStringLower(hash[..16]);
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex NamespacePattern();

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex SafePartPattern();
}
