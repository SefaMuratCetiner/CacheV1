using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CacheShowcase.Modules.Pricing.Features;

internal static class ProviderErrors
{
    /// <summary>502: the upstream failed and nothing was cached. Never cached itself (5xx gets no-store).</summary>
    public static ProblemHttpResult BadGateway(string title) =>
        TypedResults.Problem(statusCode: StatusCodes.Status502BadGateway, title: title);
}
