namespace CacheShowcase.Modules.ReferenceData.Domain;

/// <summary>A country. The list is cached under <c>reference-data.countries</c>.</summary>
public sealed record Country
{
    /// <summary>ISO 3166-1 alpha-2, uppercase.</summary>
    public required string Code { get; init; }

    public required string Name { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public required string Currency { get; init; }
}
