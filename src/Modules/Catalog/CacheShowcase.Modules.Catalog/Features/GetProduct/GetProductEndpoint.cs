using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Catalog.Data;
using FastEndpoints;

namespace CacheShowcase.Modules.Catalog.Features.GetProduct;

public sealed class GetProductRequest
{
    public int Id { get; init; }
}

/// <summary>
/// <c>GET /api/catalog/products/{id}</c>. Cache-aside with negative caching (via the decorator); HTTP validation with
/// a version ETag and <c>304 Not Modified</c>.
/// </summary>
public sealed class GetProductEndpoint(IProductReader products) : Endpoint<GetProductRequest, ProductResponse>
{
    private static readonly string FoundCacheControl = CacheControl.Public(
        maxAge: TimeSpan.FromSeconds(30),
        staleWhileRevalidate: TimeSpan.FromSeconds(30));

    private static readonly string NotFoundCacheControl = CacheControl.Public(maxAge: TimeSpan.FromSeconds(10));

    public override void Configure() => Get("/catalog/products/{id:int}");

    public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(req.Id, ct);
        if (product is null)
        {
            HttpContext.Response.Headers.CacheControl = NotFoundCacheControl;
            await Send.NotFoundAsync(ct);
            return;
        }

        var etag = ConditionalRequests.VersionETag(product.Version);
        ConditionalRequests.SetETag(HttpContext.Response, etag);
        HttpContext.Response.Headers.CacheControl = FoundCacheControl;

        if (ConditionalRequests.IsNotModified(HttpContext.Request, etag))
        {
            await Send.NotModifiedAsync(ct);
            return;
        }

        await Send.OkAsync(ProductResponse.From(product), ct);
    }
}
