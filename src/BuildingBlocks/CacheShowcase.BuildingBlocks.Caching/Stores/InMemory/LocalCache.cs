using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CacheShowcase.BuildingBlocks.Caching.Stores.InMemory;

/// <summary>
/// The in-process (L1) store: no network hop and no serialization, but each instance holds its own copy.
/// It has its own size-limited <see cref="MemoryCache"/> instead of the shared <c>IMemoryCache</c>, so it can neither
/// crowd out other users of process memory nor be crowded out by them. Entries are keyed by <c>CacheKey.Value</c>.
/// </summary>
internal sealed class LocalCache(IOptions<CachingOptions> options) : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = options.Value.LocalCacheSizeLimit });

    public bool TryGet<T>(string key, out T? value) => _cache.TryGetValue(key, out value);

    public void Set<T>(string key, T value, TimeSpan ttl) =>
        _cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl, Size = 1 });

    public void Remove(string key) => _cache.Remove(key);

    public void Dispose() => _cache.Dispose();
}
