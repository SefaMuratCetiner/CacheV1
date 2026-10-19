using CacheShowcase.Modules.Carts.Domain;

namespace CacheShowcase.Modules.Carts.Features;

public sealed record CartResponse(Guid Id, IReadOnlyList<CartItem> Items, int TotalQuantity, DateTime UpdatedAt)
{
    public static CartResponse From(Cart cart) => new(cart.Id, cart.Items, cart.Items.Sum(i => i.Quantity), cart.UpdatedAt);
}
