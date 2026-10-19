using CacheShowcase.BuildingBlocks.Caching;
using CacheShowcase.BuildingBlocks.Caching.Stores.TwoLevel;
using CacheShowcase.Modules.ReferenceData.Data;
using CacheShowcase.Modules.ReferenceData.Domain;

namespace CacheShowcase.Modules.ReferenceData.Caching;

/// <summary>
/// Reference data is small, read on almost every request and rarely changed, which is ideal for an in-process L1
/// cache in front of Valkey. Because every instance holds its own L1 copy, an update deletes the L2 entry and
/// broadcasts the L1 eviction to all instances over Valkey pub/sub. The short L1 TTL covers a missed broadcast.
/// </summary>
internal sealed class CachedCountryCatalog(CountryRepository repository, ITwoLevelCache cache) : ICountryCatalog
{
    public async Task<CacheResult<IReadOnlyList<Country>>> GetAllAsync(CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            ReferenceDataCacheKeys.AllCountries(),
            async (_, ct) => await repository.GetAllAsync(ct),
            cancellationToken);

    public async Task<Country?> UpdateAsync(string code, string name, string currency, CancellationToken cancellationToken)
    {
        var updated = await repository.UpdateAsync(code, name, currency, cancellationToken);
        if (updated is not null)
        {
            await cache.InvalidateAsync(ReferenceDataCacheKeys.AllCountries(), cancellationToken);
        }

        return updated;
    }
}
