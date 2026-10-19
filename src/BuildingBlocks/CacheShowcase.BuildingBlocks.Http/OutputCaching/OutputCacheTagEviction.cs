using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Http.OutputCaching;

/// <summary>
/// Tag-based invalidation for the output cache: responses are stored with tags (<c>policy.Tag(…)</c>), and evicting a
/// tag removes every response carrying it. The tag index lives in Valkey with the responses, so eviction reaches every
/// instance.
/// </summary>
public sealed partial class OutputCacheTagEviction(IOutputCacheStore store, ILogger<OutputCacheTagEviction> logger)
{
    /// <summary>Evicts a tag. A failure is logged, never thrown: the cached responses expire through their TTL.</summary>
    public async Task EvictAsync(string tag, CancellationToken cancellationToken)
    {
        try
        {
            await store.EvictByTagAsync(tag, cancellationToken);
            LogEvicted(tag);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            LogEvictionFailed(exception, tag);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Output cache tag {Tag} evicted")]
    private partial void LogEvicted(string tag);

    [LoggerMessage(Level = LogLevel.Error, Message = "Evicting output cache tag {Tag} failed; responses stay cached until their TTL")]
    private partial void LogEvictionFailed(Exception exception, string tag);
}
