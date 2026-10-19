namespace CacheShowcase.Modules.Analytics.Data;

/// <summary>What the Analytics endpoints need. Implemented by <c>CachedProductViewStore</c>.</summary>
public interface IProductViewStore
{
    Task RecordViewAsync(int productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TrendingProduct>> GetTrendingAsync(int limit, CancellationToken cancellationToken);
}
