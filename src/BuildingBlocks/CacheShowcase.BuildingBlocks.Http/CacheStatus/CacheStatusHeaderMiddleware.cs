using Microsoft.AspNetCore.Http;

namespace CacheShowcase.BuildingBlocks.Http.CacheStatus;

/// <summary>
/// Adds <c>X-Cache</c> to responses whose handler used the application cache. A learning and debugging aid: it shows
/// from the outside whether a request reached the source. Output-cache replays don't run the handler, so they carry
/// an <c>Age</c> header instead.
/// </summary>
public sealed class CacheStatusHeaderMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Cache";

    public Task InvokeAsync(HttpContext context, RequestCacheStatus cacheStatus)
    {
        context.Response.OnStarting(() =>
        {
            if (cacheStatus.HeaderValue is { } value)
            {
                context.Response.Headers[HeaderName] = value;
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
