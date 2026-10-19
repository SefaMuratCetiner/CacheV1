using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Catalog.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.Net.Http.Headers;

namespace CacheShowcase.Modules.Catalog.Features.GetProductImage;

public sealed class GetProductImageRequest
{
    public string Hash { get; init; } = string.Empty;
}

public sealed class GetProductImageValidator : Validator<GetProductImageRequest>
{
    public GetProductImageValidator() =>
        RuleFor(x => x.Hash).Matches("^[0-9a-f]{64}$").WithMessage("Hash must be a lowercase SHA-256 hex string.");
}

/// <summary>
/// <c>GET /api/catalog/images/{hash}</c>. Immutable, content-addressed: the URL changes whenever the content does,
/// so the server caches it without invalidation and clients may keep it for a year without revalidating.
/// </summary>
public sealed class GetProductImageEndpoint(IProductReader products) : Endpoint<GetProductImageRequest>
{
    public override void Configure() => Get("/catalog/images/{hash}");

    public override async Task HandleAsync(GetProductImageRequest req, CancellationToken ct)
    {
        var image = await products.GetImageAsync(req.Hash, ct);
        if (image is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var etag = new EntityTagHeaderValue($"\"{image.Hash}\"");
        ConditionalRequests.SetETag(HttpContext.Response, etag);
        HttpContext.Response.Headers.CacheControl = CacheControl.Immutable;

        if (ConditionalRequests.IsNotModified(HttpContext.Request, etag))
        {
            await Send.NotModifiedAsync(ct);
            return;
        }

        await Send.BytesAsync(image.Content, contentType: image.ContentType, cancellation: ct);
    }
}
