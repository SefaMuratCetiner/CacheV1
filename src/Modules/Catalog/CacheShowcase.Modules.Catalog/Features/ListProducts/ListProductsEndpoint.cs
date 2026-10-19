using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Catalog.Data;
using CacheShowcase.Modules.Catalog.Domain;
using FastEndpoints;
using FluentValidation;

namespace CacheShowcase.Modules.Catalog.Features.ListProducts;

public sealed class ListProductsRequest
{
    public string? Category { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

public sealed record ListProductsResponse(IReadOnlyList<ProductResponse> Items, int TotalCount, int Page, int PageSize);

public sealed class ListProductsValidator : Validator<ListProductsRequest>
{
    public ListProductsValidator()
    {
        RuleFor(x => x.Category!.Trim().ToLowerInvariant())
            .Must(ProductCategories.All.Contains)
            .When(x => !string.IsNullOrWhiteSpace(x.Category))
            .OverridePropertyName(nameof(ListProductsRequest.Category))
            .WithMessage("Unknown category.");
        RuleFor(x => x.Page).InclusiveBetween(1, ProductFilter.MaxPage);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ProductFilter.MaxPageSize);
    }
}

/// <summary>
/// <c>GET /api/catalog/products?category=&amp;page=&amp;pageSize=</c>. Id-list caching under a generation, then one
/// batched lookup for the products. <c>X-Cache: PARTIAL</c> shows when only some products were cached.
/// </summary>
public sealed class ListProductsEndpoint(IProductReader products) : Endpoint<ListProductsRequest, ListProductsResponse>
{
    private static readonly string CacheControlValue = CacheControl.Public(maxAge: TimeSpan.FromSeconds(15));

    public override void Configure() => Get("/catalog/products");

    public override async Task HandleAsync(ListProductsRequest req, CancellationToken ct)
    {
        var filter = ProductFilter.Create(req.Category, req.Page, req.PageSize);
        var page = await products.FindIdsAsync(filter, ct);
        var found = await products.GetManyAsync(page.Ids, ct);

        // Keep the page order; skip an id deleted between the two lookups.
        var items = page.Ids
            .Where(found.ContainsKey)
            .Select(id => ProductResponse.From(found[id]))
            .ToList();

        HttpContext.Response.Headers.CacheControl = CacheControlValue;
        await Send.OkAsync(new ListProductsResponse(items, page.TotalCount, filter.Page, filter.PageSize), ct);
    }
}
