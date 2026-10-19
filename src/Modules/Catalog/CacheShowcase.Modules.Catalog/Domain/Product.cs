namespace CacheShowcase.Modules.Catalog.Domain;

/// <summary>A product. Also the shape cached under <c>catalog.product</c>.</summary>
public sealed record Product
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Category { get; init; }

    public required decimal Price { get; init; }

    /// <summary>Incremented on every update. Drives the ETag and optimistic concurrency.</summary>
    public required int Version { get; init; }

    /// <summary>SHA-256 of the product image; the image URL is built from it.</summary>
    public required string ImageHash { get; init; }

    public required DateTime UpdatedAt { get; init; }
}
