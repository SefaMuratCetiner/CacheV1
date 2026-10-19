using CacheShowcase.Modules.Carts.Data;
using FastEndpoints;
using FluentValidation;

namespace CacheShowcase.Modules.Carts.Features.AddCartItem;

public sealed class AddCartItemRequest
{
    public Guid CartId { get; init; }

    public int ProductId { get; init; }

    public int Quantity { get; init; }
}

public sealed class AddCartItemValidator : Validator<AddCartItemRequest>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
    }
}

/// <summary>
/// <c>POST /api/carts/{cartId}/items</c>. Write-around: the database is updated and the cached cart deleted; the
/// returned cart is read back through the sliding cache, which caches it again.
/// </summary>
public sealed class AddCartItemEndpoint(ICartStore carts) : Endpoint<AddCartItemRequest, CartResponse>
{
    public override void Configure() => Post("/carts/{cartId:guid}/items");

    public override async Task HandleAsync(AddCartItemRequest req, CancellationToken ct)
    {
        await carts.AddItemAsync(req.CartId, req.ProductId, req.Quantity, ct);
        var cart = await carts.GetAsync(req.CartId, ct);
        await Send.OkAsync(CartResponse.From(cart!), ct);
    }
}
