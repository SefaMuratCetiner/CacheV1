using CacheShowcase.BuildingBlocks.Caching.Keys;
using CacheShowcase.BuildingBlocks.Http.CacheStatus;
using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.BuildingBlocks.Http.OutputCaching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching.StackExchangeRedis;
using Microsoft.AspNetCore.ResponseCompression;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpCachingExtensions
{
    /// <summary>
    /// Registers response compression, the output cache stored in Valkey (shared by every instance, same connection
    /// and key prefix as the data cache) and the per-request cache status. Modules add their own output cache policies.
    /// </summary>
    public static IServiceCollection AddHttpCaching(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddStackExchangeRedisOutputCache(_ => { });
        services.AddOptions<RedisOutputCacheOptions>()
            .Configure<CacheKeyspace, IConnectionMultiplexer>((options, keyspace, connection) =>
            {
                options.InstanceName = keyspace.ResolveInternal("output") + ":";
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connection);
            });
        services.AddOutputCache();

        services.AddSingleton<OutputCacheTagEviction>();
        services.AddScoped<RequestCacheStatus>();
        return services;
    }

    /// <summary>Adds the HTTP caching middleware. Order matters; see the comments.</summary>
    public static IApplicationBuilder UseHttpCaching(this IApplicationBuilder app)
    {
        // 1. Compression wraps the output cache: the cache stores the uncompressed body once, and each client gets
        //    the encoding it asked for (compression also adds Vary: Accept-Encoding).
        app.UseResponseCompression();

        // 2. An explicit Cache-Control on every response, and the X-Cache debug header.
        app.UseMiddleware<DefaultCacheControlMiddleware>();
        app.UseMiddleware<CacheStatusHeaderMiddleware>();

        // 3. Server-side output cache for endpoints that opt in with a policy.
        app.UseOutputCache();

        return app;
    }
}
