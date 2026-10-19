using CacheShowcase.Modules.Carts.Data;
using FastEndpoints;

namespace CacheShowcase.Modules.Carts.Features.GetCart;

public sealed class GetCartRequest
{
    public Guid CartId { get; init; }
}

/// <summary><c>GET /api/carts/{cartId}</c>. Sliding expiration: every read resets the cart's idle timer in Valkey.</summary>
public sealed class GetCartEndpoint(ICartStore carts) : Endpoint<GetCartRequest, CartResponse>
{
    public override void Configure() => Get("/carts/{cartId:guid}");

    public override async Task HandleAsync(GetCartRequest req, CancellationToken ct)
    {
        var cart = await carts.GetAsync(req.CartId, ct);
        if (cart is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(CartResponse.From(cart), ct);
    }
}
