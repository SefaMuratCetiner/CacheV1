using CacheShowcase.BuildingBlocks.Caching.Write.UpdateOnWrite;

namespace CacheShowcase.Modules.Customers.Domain;

/// <summary>
/// A customer. Also the shape cached under <c>customers.customer</c>. <see cref="IVersioned"/> lets the
/// write-through cache refuse to overwrite a newer cached version with an older one.
/// </summary>
public sealed record Customer : IVersioned
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    /// <summary>Incremented by the database on every update.</summary>
    public required long Version { get; init; }

    public required DateTime UpdatedAt { get; init; }
}
