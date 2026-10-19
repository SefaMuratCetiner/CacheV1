namespace CacheShowcase.BuildingBlocks.Caching.Read.Warming;

/// <summary>
/// Cache warming: fills a cache at startup, before traffic arrives, so the first requests after a deploy don't all
/// miss. Register with <c>AddCacheWarmer&lt;T&gt;()</c>. Worth it only for a small, known working set.
/// </summary>
public interface ICacheWarmer
{
    string Name { get; }

    Task WarmAsync(CancellationToken cancellationToken);
}
