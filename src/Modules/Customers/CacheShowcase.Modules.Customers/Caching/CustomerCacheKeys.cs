using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.Modules.Customers.Caching;

internal static class CustomerCacheKeys
{
    /// <summary>One customer by id: <c>customers.customer:v1:{id}</c>. Shared by the read-through and write-through paths.</summary>
    public const string CustomerNamespace = "customers.customer";

    public static CacheKey ById(int id) => CacheKey.Create(CustomerNamespace, 1, id);
}
