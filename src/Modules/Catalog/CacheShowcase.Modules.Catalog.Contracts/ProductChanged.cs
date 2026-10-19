using FastEndpoints;

namespace CacheShowcase.Modules.Catalog.Contracts;

/// <summary>
/// Published after a product is created or updated and the change has committed. Other modules subscribe to it to
/// invalidate their own caches (event-driven invalidation); Catalog doesn't know who listens.
/// </summary>
public sealed record ProductChanged(int ProductId) : IEvent;
