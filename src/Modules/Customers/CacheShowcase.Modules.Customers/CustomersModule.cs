using System.Reflection;
using CacheShowcase.Modules.Customers.Caching;
using CacheShowcase.Modules.Customers.Data;
using CacheShowcase.Modules.Customers.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.Customers;

/// <summary>Customers: demonstrates read-through, write-through and update-on-write with compare-and-set.</summary>
public static class CustomersModule
{
    public static Assembly Assembly => typeof(CustomersModule).Assembly;

    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddCacheNamespace(CustomerCacheKeys.CustomerNamespace, p =>
        {
            p.Ttl = TimeSpan.FromMinutes(10);
            p.NegativeTtl = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<CustomerRepository>();

        // The cache owns the loader (read-through); the same key function is used by the write-through path.
        services.AddReadThroughCache<int, Customer, CustomerLoader>(CustomerCacheKeys.ById);

        services.AddScoped<ICustomerStore, CachedCustomerStore>();
        return services;
    }
}
