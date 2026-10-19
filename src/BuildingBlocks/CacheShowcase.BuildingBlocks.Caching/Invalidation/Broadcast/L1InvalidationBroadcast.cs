using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using CacheShowcase.BuildingBlocks.Caching.Stores.InMemory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Invalidation.Broadcast;

/// <summary>
/// Pub/sub broadcast: every instance keeps its own in-process (L1) copy, so deleting from Valkey isn't enough.
/// An invalidation is published on a Valkey channel, and every instance (including this one) evicts its L1 copy.
/// </summary>
/// <remarks>
/// Pub/sub is fire-and-forget: an instance that is disconnected when the message is sent never sees it.
/// The short L1 TTL is the backstop for such missed messages.
/// </remarks>
internal sealed class L1InvalidationBroadcast(
    ValkeyStore store,
    LocalCache localCache,
    CacheKeyspace keyspace,
    CacheMetrics metrics,
    ILogger<L1InvalidationBroadcast> logger) : BackgroundService
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    private RedisChannel Channel => RedisChannel.Literal(keyspace.ResolveInternal("l1-invalidation"));

    /// <summary>Tells every instance to drop <paramref name="key"/> from its L1 cache.</summary>
    public async Task PublishAsync(CacheKey key)
    {
        if (await store.TryExecuteAsync("publish", key.Value, _ => store.Subscriber.PublishAsync(Channel, key.Value)))
        {
            metrics.RecordInvalidation(key.Namespace, CachePatterns.Broadcast);
        }
    }

    /// <summary>Subscribes once Valkey is reachable; StackExchange.Redis re-subscribes after reconnects by itself.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = Channel;
        var channelName = channel.ToString();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await store.Subscriber.SubscribeAsync(channel, (_, message) => localCache.Remove(message.ToString()));
                CacheLog.BroadcastSubscribed(logger, channelName);
                return;
            }
            catch (Exception exception) when (ValkeyStore.IsStoreFailure(exception))
            {
                CacheLog.BroadcastSubscribeFailed(logger, exception, channelName);
                await Task.Delay(RetryInterval, stoppingToken);
            }
        }
    }
}
