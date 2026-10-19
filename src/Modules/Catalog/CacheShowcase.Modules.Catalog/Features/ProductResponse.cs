using CacheShowcase.Modules.Catalog.Domain;

namespace CacheShowcase.Modules.Catalog.Features;

public sealed record ProductResponse(
    int Id,
    string Name,
    string Category,
    decimal Price,
    int Version,
    string ImageUrl,
    DateTime UpdatedAt)
{
    public static ProductResponse From(Product product) => new(
        product.Id,
        product.Name,
        product.Category,
        product.Price,
        product.Version,
        $"/api/catalog/images/{product.ImageHash}",
        product.UpdatedAt);
}
