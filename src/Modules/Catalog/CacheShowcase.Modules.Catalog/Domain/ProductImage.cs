namespace CacheShowcase.Modules.Catalog.Domain;

/// <summary>
/// An image addressed by the hash of its content. New content means a new hash and a new URL, so a cached image never
/// needs invalidating (immutable, content-addressed keys).
/// </summary>
public sealed record ProductImage
{
    public required string Hash { get; init; }

    public required string ContentType { get; init; }

#pragma warning disable CA1819 // A byte[] is the natural shape for binary content here.
    public required byte[] Content { get; init; }
#pragma warning restore CA1819
}
