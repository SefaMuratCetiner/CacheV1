namespace CacheShowcase.Modules.Catalog.Data;

/// <summary>
/// A normalized list query. Normalizing (lowercase, defaults, clamping) makes equivalent requests produce the same
/// cache key, so <c>?category=Books</c> and <c>?category=books&amp;page=1</c> share one entry.
/// </summary>
public sealed record ProductFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int MaxPage = 100;

    private ProductFilter(string? category, int page, int pageSize)
    {
        Category = category;
        Page = page;
        PageSize = pageSize;
    }

    public string? Category { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int Offset => (Page - 1) * PageSize;

    public static ProductFilter Create(string? category, int? page, int? pageSize) => new(
        string.IsNullOrWhiteSpace(category) ? null : category.Trim().ToLowerInvariant(),
        Math.Clamp(page ?? 1, 1, MaxPage),
        Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}

/// <summary>One page of matching product ids and the total number of matches.</summary>
public sealed record ProductIdPage(IReadOnlyList<int> Ids, int TotalCount);
