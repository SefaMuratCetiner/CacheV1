using CacheShowcase.Modules.Inventory.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace CacheShowcase.Modules.Inventory.Features.AdjustStock;

public sealed class AdjustStockRequest
{
    public int ProductId { get; init; }

    /// <summary>Units to add (positive) or remove (negative).</summary>
    public int Delta { get; init; }
}

public sealed class AdjustStockValidator : Validator<AdjustStockRequest>
{
    public AdjustStockValidator() => RuleFor(x => x.Delta).NotEqual(0).InclusiveBetween(-10_000, 10_000);
}

/// <summary><c>POST /api/inventory/{productId}/adjustments</c>. Writes the database, then revokes the cache entry and its lease.</summary>
public sealed class AdjustStockEndpoint(IStockStore stock) : Endpoint<AdjustStockRequest, StockResponse>
{
    public override void Configure() => Post("/inventory/{productId:int}/adjustments");

    public override async Task HandleAsync(AdjustStockRequest req, CancellationToken ct)
    {
        var result = await stock.AdjustAsync(req.ProductId, req.Delta, ct);
        switch (result.Status)
        {
            case StockAdjustmentStatus.NotFound:
                await Send.NotFoundAsync(ct);
                return;

            case StockAdjustmentStatus.InsufficientStock:
                await Send.ResultAsync(TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Not enough stock for this adjustment."));
                return;

            default:
                await Send.OkAsync(StockResponse.From(result.Level!), ct);
                return;
        }
    }
}
