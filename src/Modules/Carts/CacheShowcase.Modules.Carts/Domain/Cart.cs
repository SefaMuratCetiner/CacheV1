namespace CacheShowcase.Modules.Carts.Domain;

/// <summary>A shopping cart. The shape cached under <c>carts.cart</c>.</summary>
public sealed record Cart(Guid Id, IReadOnlyList<CartItem> Items, DateTime UpdatedAt);

public sealed record CartItem(int ProductId, int Quantity);
