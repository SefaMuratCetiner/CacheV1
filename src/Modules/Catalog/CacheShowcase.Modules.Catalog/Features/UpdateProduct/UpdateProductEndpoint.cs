using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Catalog.Contracts;
using CacheShowcase.Modules.Catalog.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace CacheShowcase.Modules.Catalog.Features.UpdateProduct;

public sealed class UpdateProductRequest
{
    public int Id { get; init; }

    /// <summary>Optional optimistic concurrency check: the ETag the client last saw.</summary>
    [FromHeader("If-Match", isRequired: false)]
    public string? IfMatch { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public decimal Price { get; init; }
}

public sealed class UpdateProductValidator : Validator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).ProductName();
        RuleFor(x => x.Category).ProductCategory();
        RuleFor(x => x.Price).ProductPrice();
    }
}

/// <summary>
/// <c>PUT /api/catalog/products/{id}</c>. Write-around + delayed double delete + generation bump (via the
/// decorator), then <see cref="ProductChanged"/>. With <c>If-Match</c>, a stale ETag gets <c>412</c>.
/// </summary>
public sealed class UpdateProductEndpoint(IProductWriter writer) : Endpoint<UpdateProductRequest, ProductResponse>
{
    public override void Configure() => Put("/catalog/products/{id:int}");

    public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
    {
        int? expectedVersion = null;
        if (req.IfMatch is not null)
        {
            if (!ConditionalRequests.TryParseVersionIfMatch(req.IfMatch, out var version))
            {
                await SendPreconditionFailedAsync();
                return;
            }

            expectedVersion = (int)version;
        }

        var result = await writer.UpdateAsync(new ProductUpdate(req.Id, req.Name, req.Category, req.Price, expectedVersion), ct);
        switch (result.Status)
        {
            case ProductUpdateStatus.NotFound:
                await Send.NotFoundAsync(ct);
                return;

            case ProductUpdateStatus.VersionConflict:
                await SendPreconditionFailedAsync();
                return;

            default:
                var product = result.Product!;
                await PublishAsync(new ProductChanged(product.Id), Mode.WaitForAll, ct);
                ConditionalRequests.SetETag(HttpContext.Response, ConditionalRequests.VersionETag(product.Version));
                await Send.OkAsync(ProductResponse.From(product), ct);
                return;
        }
    }

    private async Task SendPreconditionFailedAsync() => await Send.ResultAsync(TypedResults.Problem(
        statusCode: StatusCodes.Status412PreconditionFailed,
        title: "The product has changed since it was read.",
        detail: "GET the product again and retry with its current ETag in If-Match."));
}
