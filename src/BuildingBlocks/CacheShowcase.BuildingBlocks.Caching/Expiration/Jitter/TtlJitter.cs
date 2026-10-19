namespace CacheShowcase.BuildingBlocks.Caching.Expiration.Jitter;

/// <summary>
/// Spreads expirations so entries written together do not expire together (prevents cache avalanches).
/// Jitter only shortens a TTL, so the configured value stays a hard upper bound.
/// </summary>
internal static class TtlJitter
{
    private static readonly TimeSpan MinimumTtl = TimeSpan.FromSeconds(1);

    public static TimeSpan Apply(TimeSpan ttl, double ratio)
    {
        if (ratio <= 0)
        {
            return ttl;
        }

        // Uniform in [1 - ratio, 1]. Not security sensitive.
#pragma warning disable CA5394
        var factor = 1 - (Random.Shared.NextDouble() * ratio);
#pragma warning restore CA5394
        var jittered = ttl * factor;
        return jittered < MinimumTtl ? MinimumTtl : jittered;
    }
}
