using System.ComponentModel.DataAnnotations;

namespace CacheShowcase.Modules.Pricing.Client;

public sealed class FrankfurterOptions
{
    public const string SectionName = "Pricing:Frankfurter";

    [Required]
    public Uri BaseAddress { get; set; } = new("https://api.frankfurter.dev");

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
