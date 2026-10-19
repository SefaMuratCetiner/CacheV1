using Microsoft.Extensions.Logging;

namespace CacheShowcase.BuildingBlocks.Caching.Diagnostics;

/// <summary>Source-generated log messages. Keys are logged; cached values never are.</summary>
internal static partial class CacheLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "{Pattern} miss for {CacheKey}")]
    public static partial void Miss(ILogger logger, string pattern, string cacheKey);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "{Pattern} invalidated {CacheKey}")]
    public static partial void Invalidated(ILogger logger, string pattern, string cacheKey);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "Generation of {Namespace} advanced to {Generation}")]
    public static partial void GenerationAdvanced(ILogger logger, string @namespace, long generation);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Background refresh scheduled for {CacheKey} ({Reason})")]
    public static partial void RefreshScheduled(ILogger logger, string cacheKey, string reason);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug, Message = "Probabilistic early recompute of {CacheKey}, {Remaining} before expiry")]
    public static partial void EarlyRecompute(ILogger logger, string cacheKey, TimeSpan remaining);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Lease for {CacheKey} was revoked while loading; the loaded value was not cached")]
    public static partial void LeaseRevoked(ILogger logger, string cacheKey);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Debug, Message = "Versioned write to {CacheKey} skipped: the cache already holds version {CachedVersion} or newer")]
    public static partial void VersionedWriteSkipped(ILogger logger, string cacheKey, long cachedVersion);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Write-behind buffer {Buffer} flushed {Entries} entries")]
    public static partial void WriteBehindFlushed(ILogger logger, string buffer, int entries);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Cache warmer {Warmer} finished in {Elapsed}")]
    public static partial void WarmerFinished(ILogger logger, string warmer, TimeSpan elapsed);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Information, Message = "Subscribed to L1 invalidation channel {Channel}")]
    public static partial void BroadcastSubscribed(ILogger logger, string channel);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Warning, Message = "Cache {Operation} failed for {CacheKey}; continuing without the cache")]
    public static partial void OperationFailed(ILogger logger, Exception exception, string operation, string cacheKey);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning, Message = "Cached value for {CacheKey} could not be deserialized; treating it as a miss")]
    public static partial void DeserializationFailed(ILogger logger, Exception exception, string cacheKey);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Warning, Message = "Background refresh of {CacheKey} failed; the cached value is kept")]
    public static partial void BackgroundRefreshFailed(ILogger logger, Exception exception, string cacheKey);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Warning, Message = "Cache warmer {Warmer} failed; the cache fills lazily instead")]
    public static partial void WarmerFailed(ILogger logger, Exception exception, string warmer);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Error, Message = "Write-behind flush of {Buffer} failed; the buffer is kept and retried")]
    public static partial void WriteBehindFlushFailed(ILogger logger, Exception exception, string buffer);

    [LoggerMessage(EventId = 1015, Level = LogLevel.Warning, Message = "Valkey unavailable; write-behind {Buffer} wrote {Member} straight to the source")]
    public static partial void WriteBehindFallback(ILogger logger, string buffer, string member);

    [LoggerMessage(EventId = 1016, Level = LogLevel.Warning, Message = "Could not subscribe to L1 invalidation channel {Channel}; retrying")]
    public static partial void BroadcastSubscribeFailed(ILogger logger, Exception exception, string channel);
}
