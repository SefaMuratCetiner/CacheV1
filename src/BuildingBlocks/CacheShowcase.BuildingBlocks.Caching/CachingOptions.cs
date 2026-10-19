using System.ComponentModel.DataAnnotations;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;

namespace CacheShowcase.BuildingBlocks.Caching;

/// <summary>
/// Global settings, bound from the <c>Caching</c> section and read through <c>IOptionsMonitor</c>,
/// so changes (including the kill switch) apply without a restart.
/// </summary>
public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>Kill switch. When false every pattern goes straight to the source.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Application name used as the first key segment: <c>{KeyPrefix}:{environment}:…</c>.</summary>
    [Required]
    [RegularExpression("^[a-z0-9-]+$")]
    public string KeyPrefix { get; set; } = "cacheshowcase";

    /// <summary>Up to this fraction is randomly taken off each distributed TTL (0.1 = up to 10% shorter).</summary>
    [Range(0, 0.5)]
    public double JitterRatio { get; set; } = 0.1;

    /// <summary>Lifetime of generation counters; must outlive the longest TTL in any generation-keyed namespace.</summary>
    public TimeSpan GenerationTtl { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Delay before the second delete of the double-delete pattern.</summary>
    public TimeSpan DoubleDeleteDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Maximum number of entries in the in-process (L1) cache.</summary>
    [Range(1, 1_000_000)]
    public long LocalCacheSizeLimit { get; set; } = 1_000;

    /// <summary>
    /// TTL policies per namespace. Modules register their defaults with <c>AddCacheNamespace</c>;
    /// <c>Caching:Namespaces:{name}</c> in configuration overrides individual values.
    /// </summary>
    public Dictionary<string, CacheNamespacePolicy> Namespaces { get; } = new(StringComparer.Ordinal);

    /// <exception cref="InvalidOperationException">No policy is registered for the namespace.</exception>
    public CacheNamespacePolicy GetPolicy(string cacheNamespace) =>
        Namespaces.TryGetValue(cacheNamespace, out var policy)
            ? policy
            : throw new InvalidOperationException(
                $"No cache policy for namespace '{cacheNamespace}'. Register one with AddCacheNamespace.");
}
