namespace CacheShowcase.BuildingBlocks.Caching.Diagnostics;

/// <summary>Values of the <c>cache.pattern</c> metric tag, so dashboards can break results down by pattern.</summary>
public static class CachePatterns
{
    public const string CacheAside = "cache-aside";
    public const string ReadThrough = "read-through";
    public const string RefreshAhead = "refresh-ahead";
    public const string Memoization = "memoization";
    public const string Local = "local";
    public const string Sliding = "sliding";
    public const string SoftHard = "soft-hard";
    public const string EarlyExpiration = "early-expiration";
    public const string Lease = "lease";
    public const string WriteThrough = "write-through";
    public const string WriteBehind = "write-behind";
    public const string KeyDelete = "key-delete";
    public const string Generation = "generation";
    public const string DoubleDelete = "double-delete";
    public const string Broadcast = "broadcast";
}
