using CacheShowcase.BuildingBlocks.Caching.Read.Warming;
using CacheShowcase.Modules.ReferenceData.Data;

namespace CacheShowcase.Modules.ReferenceData.Caching;

/// <summary>Cache warming: loads the country list into L1 and L2 at startup, so the first request is already a hit.</summary>
internal sealed class CountriesWarmer(ICountryCatalog countries) : ICacheWarmer
{
    public string Name => "reference-data.countries";

    public Task WarmAsync(CancellationToken cancellationToken) => countries.GetAllAsync(cancellationToken);
}
