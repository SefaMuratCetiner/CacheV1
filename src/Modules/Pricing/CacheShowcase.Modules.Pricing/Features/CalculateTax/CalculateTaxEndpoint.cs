using CacheShowcase.BuildingBlocks.Caching.Read.Memoization;
using CacheShowcase.BuildingBlocks.Http.Headers;
using CacheShowcase.Modules.Pricing.Tax;
using FastEndpoints;
using FluentValidation;

namespace CacheShowcase.Modules.Pricing.Features.CalculateTax;

public sealed class CalculateTaxRequest
{
    public string Country { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}

public sealed record CalculateTaxResponse(string Country, string Category, decimal Amount, decimal Rate, decimal Tax, decimal Total);

public sealed class CalculateTaxValidator : Validator<CalculateTaxRequest>
{
    public CalculateTaxValidator()
    {
        RuleFor(x => x.Country.ToUpperInvariant())
            .Must(TaxRules.Countries.Contains)
            .OverridePropertyName(nameof(CalculateTaxRequest.Country))
            .WithMessage($"Country must be one of: {string.Join(", ", TaxRules.Countries)}.");
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(1_000_000_000);
    }
}

/// <summary>
/// <c>GET /api/pricing/tax?country=DE&amp;category=books&amp;amount=100</c>. The rate lookup is memoized in process;
/// watch the <c>memoization</c> hits in the metrics. The response depends only on the query, so HTTP caches may keep
/// it for an hour.
/// </summary>
public sealed class CalculateTaxEndpoint(IMemoizer<TaxRateQuery, decimal> taxRate) : Endpoint<CalculateTaxRequest, CalculateTaxResponse>
{
    private static readonly string CacheControlValue = CacheControl.Public(maxAge: TimeSpan.FromHours(1));

    public override void Configure() => Get("/pricing/tax");

    public override async Task HandleAsync(CalculateTaxRequest req, CancellationToken ct)
    {
        var query = new TaxRateQuery(req.Country.ToUpperInvariant(), req.Category.Trim().ToLowerInvariant());
        var rate = taxRate.Invoke(query);
        var tax = Math.Round(req.Amount * rate, 2, MidpointRounding.ToEven);

        HttpContext.Response.Headers.CacheControl = CacheControlValue;
        await Send.OkAsync(new CalculateTaxResponse(query.Country, query.Category, req.Amount, rate, tax, req.Amount + tax), ct);
    }
}
