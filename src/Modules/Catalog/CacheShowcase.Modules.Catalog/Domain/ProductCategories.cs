namespace CacheShowcase.Modules.Catalog.Domain;

/// <summary>The fixed set of categories. Lowercase slugs, which keeps cache keys canonical.</summary>
public static class ProductCategories
{
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(["books", "electronics", "food", "garden", "toys"], StringComparer.Ordinal);
}
