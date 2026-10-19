using CacheShowcase.BuildingBlocks.Caching;
using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Policies;
using CacheShowcase.BuildingBlocks.Caching.Expiration.ProbabilisticEarly;
using CacheShowcase.BuildingBlocks.Caching.Expiration.Sliding;
using CacheShowcase.BuildingBlocks.Caching.Expiration.SoftHard;
using CacheShowcase.BuildingBlocks.Caching.Internal;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.Broadcast;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.DoubleDelete;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.Generations;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.KeyDelete;
using CacheShowcase.BuildingBlocks.Caching.Invalidation.Leases;
using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Caching.Read.CacheAside;
using CacheShowcase.BuildingBlocks.Caching.Read.Memoization;
using CacheShowcase.BuildingBlocks.Caching.Read.ReadThrough;
using CacheShowcase.BuildingBlocks.Caching.Read.RefreshAhead;
using CacheShowcase.BuildingBlocks.Caching.Read.Warming;
using CacheShowcase.BuildingBlocks.Caching.Serialization;
using CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;
using CacheShowcase.BuildingBlocks.Caching.Stores.InMemory;
using CacheShowcase.BuildingBlocks.Caching.Stores.TwoLevel;
using CacheShowcase.BuildingBlocks.Caching.Write.UpdateOnWrite;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteAround;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;
using CacheShowcase.BuildingBlocks.Caching.Write.WriteThrough;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

// Lives in Microsoft's namespace by convention, so these methods show up wherever IServiceCollection does.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registration API of the caching building block: <c>AddCaching</c> once, the rest per module.</summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Valkey connection and every pattern's mechanism. Reads the <c>Caching</c> section and the
    /// <c>Valkey</c> connection string.
    /// </summary>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CachingOptions>()
            .Bind(configuration.GetSection(CachingOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => o.Namespaces.All(n => !n.Value.Validate(n.Key).Any()),
                "A cache namespace policy is invalid; see CacheNamespacePolicy.Validate.")
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("Valkey")
            ?? throw new InvalidOperationException("Connection string 'Valkey' is missing.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redis = ConfigurationOptions.Parse(connectionString);

            // Start even when Valkey is down and keep reconnecting in the background.
            redis.AbortOnConnectFail = false;

            // While disconnected, fail commands at once instead of queueing them until they time out,
            // so requests fall back to the source without waiting.
            redis.BacklogPolicy = BacklogPolicy.FailFast;

            return ConnectionMultiplexer.Connect(redis);
        });

        services.TryAddSingleton(TimeProvider.System);

        // Infrastructure shared by the patterns.
        services.AddSingleton<CacheKeyspace>();
        services.AddSingleton<CacheMetrics>();
        services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
        services.AddSingleton<CacheEntryCodec>();
        services.AddSingleton<ValkeyStore>();
        services.AddSingleton<LocalCache>();
        services.AddSingleton<SingleFlight>();
        services.AddSingleton<BackgroundWork>();

        // Read patterns.
        services.AddSingleton<CacheAsideCache>();
        services.AddSingleton<ICacheAside>(sp => sp.GetRequiredService<CacheAsideCache>());
        services.AddSingleton<IRefreshAheadCache, RefreshAheadCache>();
        services.AddHostedService<CacheWarmupService>();

        // Write patterns.
        services.AddSingleton<IWriteAround, WriteAround>();
        services.AddSingleton<IVersionedCacheWriter, VersionedCacheWriter>();
        services.AddSingleton<IWriteThroughCache, WriteThroughCache>();

        // Invalidation patterns.
        services.AddSingleton<CacheInvalidator>();
        services.AddSingleton<ICacheInvalidator>(sp => sp.GetRequiredService<CacheInvalidator>());
        services.AddSingleton<ICacheGenerations, CacheGenerations>();
        services.AddSingleton<IDoubleDelete, DoubleDelete>();
        services.AddSingleton<ILeasedCache, LeasedCache>();
        services.AddSingleton<L1InvalidationBroadcast>();
        services.AddHostedService(sp => sp.GetRequiredService<L1InvalidationBroadcast>());

        // Expiration patterns.
        services.AddSingleton<ISlidingCache, SlidingCache>();
        services.AddSingleton<ISoftHardCache, SoftHardCache>();
        services.AddSingleton<IEarlyExpirationCache, EarlyExpirationCache>();

        // Stores.
        services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

        return services;
    }

    /// <summary>
    /// Declares a namespace and its default expiration policy. The owning module calls this; configuration under
    /// <c>Caching:Namespaces:{name}</c> overrides individual values, and changes apply without a restart.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cacheNamespace"><c>module.name</c>, e.g. <c>catalog.product</c>.</param>
    /// <param name="defaults">Sets the default policy values.</param>
    public static IServiceCollection AddCacheNamespace(
        this IServiceCollection services,
        string cacheNamespace,
        Action<CacheNamespacePolicy> defaults)
    {
        CacheKey.EnsureValidNamespace(cacheNamespace);

        services.AddOptions<CachingOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            var policy = new CacheNamespacePolicy();
            defaults(policy);
            configuration.GetSection($"{CachingOptions.SectionName}:Namespaces:{cacheNamespace}").Bind(policy);
            options.Namespaces[cacheNamespace] = policy;
        });

        return services;
    }

    /// <summary>Registers a read-through cache whose misses are loaded by <typeparamref name="TLoader"/>.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="keyFor">Builds the cache key for a lookup key. Writers of the same data must use the same function.</param>
    public static IServiceCollection AddReadThroughCache<TKey, TValue, TLoader>(
        this IServiceCollection services,
        Func<TKey, CacheKey> keyFor)
        where TValue : class
        where TLoader : class, ICacheLoader<TKey, TValue>
    {
        services.AddSingleton<TLoader>();
        services.AddSingleton<IReadThroughCache<TKey, TValue>>(sp => new ReadThroughCache<TKey, TValue>(
            sp.GetRequiredService<CacheAsideCache>(),
            sp.GetRequiredService<TLoader>(),
            keyFor));
        return services;
    }

    /// <summary>Registers a warmer that runs at startup, before the first request.</summary>
    public static IServiceCollection AddCacheWarmer<TWarmer>(this IServiceCollection services)
        where TWarmer : class, ICacheWarmer =>
        services.AddSingleton<ICacheWarmer, TWarmer>();

    /// <summary>Registers <see cref="IMemoizer{TArg,TResult}"/> for a pure function.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">Name used in metrics.</param>
    /// <param name="function">The pure function to memoize.</param>
    /// <param name="sizeLimit">Maximum number of remembered results.</param>
    public static IServiceCollection AddMemoizer<TArg, TResult>(
        this IServiceCollection services,
        string name,
        Func<TArg, TResult> function,
        long sizeLimit)
        where TArg : notnull =>
        services.AddSingleton<IMemoizer<TArg, TResult>>(sp =>
            new Memoizer<TArg, TResult>(name, function, sizeLimit, sp.GetRequiredService<CacheMetrics>()));

    /// <summary>
    /// Registers a write-behind counter buffer flushed to <typeparamref name="TSink"/> every
    /// <paramref name="flushInterval"/>, plus its background flusher.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">Buffer name, used in keys and metrics.</param>
    /// <param name="flushInterval">How often buffered increments are written to the source; also the data-loss window.</param>
    public static IServiceCollection AddWriteBehindCounter<TSink>(
        this IServiceCollection services,
        string name,
        TimeSpan flushInterval)
        where TSink : class, IWriteBehindSink
    {
        services.AddSingleton<TSink>();
        services.AddSingleton(sp => new WriteBehindBuffer<TSink>(name, flushInterval, sp.GetRequiredService<CacheKeyspace>()));
        services.AddSingleton<IWriteBehindCounter<TSink>, WriteBehindCounter<TSink>>();
        services.AddHostedService<WriteBehindFlusher<TSink>>();
        return services;
    }
}
