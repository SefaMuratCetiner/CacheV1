using CacheShowcase.Modules.Catalog.Domain;

namespace CacheShowcase.Modules.Catalog.Data;

/// <summary>Product writes. The repository implements it; <c>CachedProductWriter</c> adds cache invalidation.</summary>
public interface IProductWriter
{
    Task<Product> CreateAsync(NewProduct product, CancellationToken cancellationToken);

    Task<ProductUpdateResult> UpdateAsync(ProductUpdate update, CancellationToken cancellationToken);
}

public sealed record NewProduct(string Name, string Category, decimal Price);

/// <param name="ExpectedVersion">Optimistic concurrency: update only if the stored version still matches.</param>
public sealed record ProductUpdate(int Id, string Name, string Category, decimal Price, int? ExpectedVersion);

public enum ProductUpdateStatus
{
    Updated,
    NotFound,
    VersionConflict,
}

public sealed record ProductUpdateResult(ProductUpdateStatus Status, Product? Product = null);
