using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Customers.Data;
using FastEndpoints;

namespace CacheShowcase.Modules.Customers.Features.GetCustomer;

public sealed class GetCustomerRequest
{
    public int Id { get; init; }
}

/// <summary>
/// <c>GET /api/customers/{id}</c>. Read-through. Customer data is personal, so HTTP caching is <c>private</c>:
/// the user's browser may keep it briefly, shared caches never.
/// </summary>
public sealed class GetCustomerEndpoint(ICustomerStore customers) : Endpoint<GetCustomerRequest, CustomerResponse>
{
    private static readonly string CacheControlValue = CacheControl.Private(TimeSpan.FromSeconds(10));

    public override void Configure() => Get("/customers/{id:int}");

    public override async Task HandleAsync(GetCustomerRequest req, CancellationToken ct)
    {
        var customer = await customers.GetAsync(req.Id, ct);
        if (customer is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        HttpContext.Response.Headers.CacheControl = CacheControlValue;
        ConditionalRequests.SetETag(HttpContext.Response, ConditionalRequests.VersionETag(customer.Version));
        await Send.OkAsync(CustomerResponse.From(customer), ct);
    }
}
