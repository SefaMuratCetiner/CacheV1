using System.Security;
using System.Security.Cryptography;
using System.Text;
using CacheShowcase.Modules.Catalog.Domain;

namespace CacheShowcase.Modules.Catalog.Images;

/// <summary>
/// Renders a placeholder SVG for a product. Deterministic: the same name and category always give the same bytes and
/// therefore the same hash, so an update that doesn't change them keeps the same image URL.
/// </summary>
internal static class ProductImageRenderer
{
    public static ProductImage Render(string name, string category)
    {
        var color = category switch
        {
            "books" => "#4f46e5",
            "electronics" => "#0891b2",
            "food" => "#ca8a04",
            "garden" => "#16a34a",
            _ => "#db2777",
        };

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="240" height="160" viewBox="0 0 240 160"><rect width="240" height="160" fill="{color}"/><text x="120" y="86" font-family="sans-serif" font-size="16" fill="#fff" text-anchor="middle">{SecurityElement.Escape(name)}</text></svg>
            """;

        var content = Encoding.UTF8.GetBytes(svg);
        return new ProductImage
        {
            Hash = Convert.ToHexStringLower(SHA256.HashData(content)),
            ContentType = "image/svg+xml",
            Content = content,
        };
    }
}
