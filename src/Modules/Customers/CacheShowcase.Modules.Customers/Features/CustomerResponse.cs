using CacheShowcase.Modules.Customers.Domain;

namespace CacheShowcase.Modules.Customers.Features;

public sealed record CustomerResponse(int Id, string Name, string Email, long Version, DateTime UpdatedAt)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Version, customer.UpdatedAt);
}
