using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.Modules.ReferenceData.Caching;

internal static class ReferenceDataCacheKeys
{
    /// <summary>All countries (two-level: L1 in process, L2 in Valkey): <c>reference-data.countries:v1:all</c>.</summary>
    public const string CountriesNamespace = "reference-data.countries";

    public static CacheKey AllCountries() => CacheKey.Create(CountriesNamespace, 1, "all");
}
