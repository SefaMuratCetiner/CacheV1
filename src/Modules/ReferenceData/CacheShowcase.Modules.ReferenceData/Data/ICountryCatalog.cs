using CacheShowcase.BuildingBlocks.Caching;
using CacheShowcase.Modules.ReferenceData.Domain;

namespace CacheShowcase.Modules.ReferenceData.Data;

/// <summary>What the ReferenceData endpoints and the warmer need. Implemented by <c>CachedCountryCatalog</c>.</summary>
public interface ICountryCatalog
{
    Task<CacheResult<IReadOnlyList<Country>>> GetAllAsync(CancellationToken cancellationToken);

    /// <returns>The updated country, or null when the code is unknown.</returns>
    Task<Country?> UpdateAsync(string code, string name, string currency, CancellationToken cancellationToken);
}
