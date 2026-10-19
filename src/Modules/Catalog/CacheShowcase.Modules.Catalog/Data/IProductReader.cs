using CacheShowcase.Modules.Catalog.Domain;

namespace CacheShowcase.Modules.Catalog.Data;

/// <summary>Product reads. The repository implements it; <c>CachedProductReader</c> decorates it with caching.</summary>
public interface IProductReader
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, Product>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    Task<ProductIdPage> FindIdsAsync(ProductFilter filter, CancellationToken cancellationToken);

    Task<ProductImage?> GetImageAsync(string hash, CancellationToken cancellationToken);
}
