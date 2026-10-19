using CacheShowcase.Modules.Analytics.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace CacheShowcase.Modules.Analytics.Features.RecordProductView;

public sealed class RecordProductViewRequest
{
    public int ProductId { get; init; }
}

public sealed class RecordProductViewValidator : Validator<RecordProductViewRequest>
{
    public RecordProductViewValidator() => RuleFor(x => x.ProductId).GreaterThan(0);
}

/// <summary>
/// <c>POST /api/analytics/products/{productId}/views</c>. Write-behind: one <c>HINCRBY</c> in Valkey; the database
/// receives the totals in a batch every few seconds. <c>202 Accepted</c>, because the view isn't in the database yet.
/// </summary>
public sealed class RecordProductViewEndpoint(IProductViewStore views) : Endpoint<RecordProductViewRequest>
{
    public override void Configure() => Post("/analytics/products/{productId:int}/views");

    public override async Task HandleAsync(RecordProductViewRequest req, CancellationToken ct)
    {
        await views.RecordViewAsync(req.ProductId, ct);
        await Send.ResultAsync(TypedResults.Accepted((string?)null));
    }
}
