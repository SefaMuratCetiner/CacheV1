using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Write.UpdateOnWrite;

/// <summary>
/// Update-on-write with compare-and-set: overwrites the cached value only when the new value's version is higher
/// than the cached one. Plain "SET on write" lets two concurrent writers store their values in the opposite order
/// to the database, leaving the older value cached; the version check makes the order irrelevant.
/// </summary>
public interface IVersionedCacheWriter
{
    Task<VersionedWriteResult> SetIfNewerAsync<T>(CacheKey key, T value, CancellationToken cancellationToken = default)
        where T : class, IVersioned;
}

public enum VersionedWriteResult
{
    /// <summary>The cache now holds this version.</summary>
    Stored,

    /// <summary>The cache already held the same or a newer version, so it was left alone.</summary>
    SkippedOlder,

    /// <summary>Valkey was unavailable or caching is disabled.</summary>
    Failed,
}
