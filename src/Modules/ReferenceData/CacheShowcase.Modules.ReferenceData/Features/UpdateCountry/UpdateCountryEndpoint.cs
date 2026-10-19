using CacheShowcase.Modules.ReferenceData.Data;
using CacheShowcase.Modules.ReferenceData.Domain;
using FastEndpoints;
using FluentValidation;

namespace CacheShowcase.Modules.ReferenceData.Features.UpdateCountry;

public sealed class UpdateCountryRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Currency { get; init; } = string.Empty;
}

public sealed class UpdateCountryValidator : Validator<UpdateCountryRequest>
{
    public UpdateCountryValidator()
    {
        RuleFor(x => x.Code).Matches("^[A-Za-z]{2}$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Currency).Matches("^[A-Za-z]{3}$");
    }
}

/// <summary>
/// <c>PUT /api/reference-data/countries/{code}</c>. Deletes the L2 entry and broadcasts the L1 eviction to every
/// instance.
/// </summary>
public sealed class UpdateCountryEndpoint(ICountryCatalog countries) : Endpoint<UpdateCountryRequest, Country>
{
    public override void Configure() => Put("/reference-data/countries/{code}");

    public override async Task HandleAsync(UpdateCountryRequest req, CancellationToken ct)
    {
        var country = await countries.UpdateAsync(req.Code.ToUpperInvariant(), req.Name, req.Currency.ToUpperInvariant(), ct);
        if (country is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(country, ct);
    }
}
