using CacheShowcase.Modules.Catalog.Domain;
using FluentValidation;

namespace CacheShowcase.Modules.Catalog.Features;

/// <summary>Validation rules shared by the create and update endpoints.</summary>
internal static class ProductRules
{
    public static IRuleBuilderOptions<T, string> ProductName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(100);

    public static IRuleBuilderOptions<T, string> ProductCategory<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(ProductCategories.All.Contains)
            .WithMessage($"Category must be one of: {string.Join(", ", ProductCategories.All)}.");

    public static IRuleBuilderOptions<T, decimal> ProductPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0).LessThan(1_000_000);
}
