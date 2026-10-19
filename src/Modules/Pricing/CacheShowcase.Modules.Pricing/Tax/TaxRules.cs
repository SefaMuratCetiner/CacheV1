using System.Collections.Frozen;

namespace CacheShowcase.Modules.Pricing.Tax;

/// <summary>Which VAT rate applies to a category in a country.</summary>
public readonly record struct TaxRateQuery(string Country, string Category);

/// <summary>
/// A small, fixed VAT rule set. <see cref="ResolveRate"/> is a pure function (same input, same output, no I/O), which
/// is what makes memoization safe: a remembered result can never become stale. The rules are simplified examples,
/// not tax advice.
/// </summary>
public static class TaxRules
{
    private static readonly FrozenSet<string> ReducedRateCategories = FrozenSet.Create(StringComparer.Ordinal, "books", "food");

    private static readonly FrozenDictionary<string, (decimal Standard, decimal Reduced)> Rates =
        new Dictionary<string, (decimal, decimal)>(StringComparer.Ordinal)
        {
            ["DE"] = (0.19m, 0.07m),
            ["FR"] = (0.20m, 0.055m),
            ["NL"] = (0.21m, 0.09m),
            ["GB"] = (0.20m, 0m),
            ["TR"] = (0.20m, 0.10m),
            ["US"] = (0m, 0m),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IEnumerable<string> Countries => Rates.Keys;

    public static decimal ResolveRate(TaxRateQuery query)
    {
        var (standard, reduced) = Rates[query.Country];
        return ReducedRateCategories.Contains(query.Category) ? reduced : standard;
    }
}
