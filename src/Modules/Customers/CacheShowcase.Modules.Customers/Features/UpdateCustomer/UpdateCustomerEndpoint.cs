using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Customers.Data;
using FastEndpoints;
using FluentValidation;

namespace CacheShowcase.Modules.Customers.Features.UpdateCustomer;

public sealed class UpdateCustomerRequest
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class UpdateCustomerValidator : Validator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}

/// <summary><c>PUT /api/customers/{id}</c>. Write-through with a versioned compare-and-set.</summary>
public sealed class UpdateCustomerEndpoint(ICustomerStore customers) : Endpoint<UpdateCustomerRequest, CustomerResponse>
{
    public override void Configure() => Put("/customers/{id:int}");

    public override async Task HandleAsync(UpdateCustomerRequest req, CancellationToken ct)
    {
        var customer = await customers.UpdateAsync(req.Id, req.Name, req.Email, ct);
        if (customer is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        ConditionalRequests.SetETag(HttpContext.Response, ConditionalRequests.VersionETag(customer.Version));
        await Send.OkAsync(CustomerResponse.From(customer), ct);
    }
}
