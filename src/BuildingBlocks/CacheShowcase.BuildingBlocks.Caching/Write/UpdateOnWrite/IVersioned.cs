namespace CacheShowcase.BuildingBlocks.Caching.Write.UpdateOnWrite;

/// <summary>
/// A value carrying a version that increases with every change (e.g. a row version column).
/// It is serialized as <c>"version"</c>, which the compare-and-set script reads.
/// </summary>
public interface IVersioned
{
    long Version { get; }
}
