using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Keys;

/// <summary>
/// Owns the <c>{app}:{environment}:</c> prefix of every key this application stores, so applications and
/// environments sharing one Valkey can never read each other's entries.
/// </summary>
public sealed class CacheKeyspace(IOptions<CachingOptions> options, IHostEnvironment environment)
{
    /// <summary>The prefix, e.g. <c>cacheshowcase:development:</c>.</summary>
    public string Prefix { get; } = $"{options.Value.KeyPrefix}:{environment.EnvironmentName.ToLowerInvariant()}:";

    /// <summary>The full key stored in Valkey.</summary>
    public string Resolve(CacheKey key) => Prefix + key.Value;

    /// <summary>A key that belongs to an entry, such as its lease: <c>{entry key}:{suffix}</c>.</summary>
    public string ResolveCompanion(CacheKey key, string suffix) => $"{Prefix}{key.Value}:{suffix}";

    /// <summary>A key for infrastructure state that is not a cache entry, such as a generation counter.</summary>
    public string ResolveInternal(params ReadOnlySpan<string> segments) => Prefix + string.Join(':', segments.ToArray());
}
