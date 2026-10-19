using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Catalog.Contracts;
using CacheShowcase.Modules.Catalog.Data;
using CacheShowcase.Modules.Catalog.Features.GetProduct;
using FastEndpoints;

namespace CacheShowcase.Modules.Catalog.Features.CreateProduct;

public sealed class CreateProductRequest
{
    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public decimal Price { get; init; }
}

public sealed class CreateProductValidator : Validator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).ProductName();
        RuleFor(x => x.Category).ProductCategory();
        RuleFor(x => x.Price).ProductPrice();
    }
}

/// <summary>
/// <c>POST /api/catalog/products</c>. Write-around + key delete (via the decorator), then <see cref="ProductChanged"/>
/// for other modules. <c>WaitForAll</c>: the caller's next read must not see stale data anywhere.
/// </summary>
public sealed class CreateProductEndpoint(IProductWriter writer) : Endpoint<CreateProductRequest, ProductResponse>
{
    public override void Configure() => Post("/catalog/products");

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var product = await writer.CreateAsync(new NewProduct(req.Name, req.Category, req.Price), ct);
        await PublishAsync(new ProductChanged(product.Id), Mode.WaitForAll, ct);

        ConditionalRequests.SetETag(HttpContext.Response, ConditionalRequests.VersionETag(product.Version));
        await Send.CreatedAtAsync<GetProductEndpoint>(new { id = product.Id }, ProductResponse.From(product), cancellation: ct);
    }
}
