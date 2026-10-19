using Microsoft.AspNetCore.Http;

namespace CacheShowcase.BuildingBlocks.Http.Headers;

/// <summary>
/// Makes HTTP caching opt-in: a response without <c>Cache-Control</c> gets <c>no-store</c>, and so does every 5xx,
/// even if the handler had set a cacheable value. Nothing is cached by accident, and errors are never cached.
/// </summary>
public sealed class DefaultCacheControlMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var response = context.Response;
            if (response.StatusCode >= StatusCodes.Status500InternalServerError
                || string.IsNullOrEmpty(response.Headers.CacheControl))
            {
                response.Headers.CacheControl = CacheControl.NoStore;
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
