namespace CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;

/// <summary>
/// Expiration settings for one namespace. <see cref="Ttl"/> applies to every pattern; the optional knobs are
/// required only by the pattern that uses them, and that pattern fails fast when its knob is missing.
/// </summary>
public sealed class CacheNamespacePolicy
{
    /// <summary>Absolute (hard) lifetime. Also the cap for sliding entries and the hard TTL of soft/hard entries.</summary>
    public TimeSpan Ttl { get; set; }

    /// <summary>Lifetime of a cached "not found". Null disables negative caching for the namespace.</summary>
    public TimeSpan? NegativeTtl { get; set; }

    /// <summary>Soft/hard TTL: how long an entry is fresh. Between this and <see cref="Ttl"/> it is served stale while refreshing.</summary>
    public TimeSpan? SoftTtl { get; set; }

    /// <summary>Sliding expiration: the idle time after which an entry expires.</summary>
    public TimeSpan? SlidingTtl { get; set; }

    /// <summary>Two-level cache: lifetime in the in-process (L1) cache.</summary>
    public TimeSpan? LocalTtl { get; set; }

    /// <summary>Refresh-ahead: fraction of the TTL after which a read triggers a background refresh (0.8 = at 80%).</summary>
    public double? RefreshAheadRatio { get; set; }

    /// <summary>Probabilistic early expiration (XFetch): higher values recompute earlier. 1.0 is the usual default.</summary>
    public double? EarlyExpirationBeta { get; set; }

    internal static T Require<T>(T? value, string cacheNamespace, string setting)
        where T : struct =>
        value ?? throw new InvalidOperationException(
            $"Cache namespace '{cacheNamespace}' needs '{setting}' for the pattern that uses it.");

    internal IEnumerable<string> Validate(string cacheNamespace)
    {
        if (Ttl <= TimeSpan.Zero)
        {
            yield return $"{cacheNamespace}: Ttl must be positive.";
        }

        if (NegativeTtl is { } negative && negative <= TimeSpan.Zero)
        {
            yield return $"{cacheNamespace}: NegativeTtl must be positive when set.";
        }

        if (SoftTtl is { } soft && (soft <= TimeSpan.Zero || soft >= Ttl))
        {
            yield return $"{cacheNamespace}: SoftTtl must be positive and shorter than Ttl.";
        }

        if (SlidingTtl is { } sliding && (sliding <= TimeSpan.Zero || sliding > Ttl))
        {
            yield return $"{cacheNamespace}: SlidingTtl must be positive and no longer than Ttl.";
        }

        if (LocalTtl is { } local && (local <= TimeSpan.Zero || local > Ttl))
        {
            yield return $"{cacheNamespace}: LocalTtl must be positive and no longer than Ttl.";
        }

        if (RefreshAheadRatio is { } ratio && ratio is <= 0 or >= 1)
        {
            yield return $"{cacheNamespace}: RefreshAheadRatio must be between 0 and 1.";
        }

        if (EarlyExpirationBeta is { } beta && beta <= 0)
        {
            yield return $"{cacheNamespace}: EarlyExpirationBeta must be positive.";
        }
    }
}
