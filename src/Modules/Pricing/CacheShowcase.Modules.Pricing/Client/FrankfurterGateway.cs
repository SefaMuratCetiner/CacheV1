using Microsoft.Extensions.DependencyInjection;

namespace CacheShowcase.Modules.Pricing.Client;

/// <summary>
/// Calls the Refit client from a fresh DI scope. Loaders passed to refresh-ahead and soft/hard caches also run in
/// the background, after the request that scheduled them has ended, so they must not capture request-scoped
/// services such as the typed <see cref="HttpClient"/> behind the Refit client.
/// </summary>
internal sealed class FrankfurterGateway(IServiceScopeFactory scopes)
{
    public async Task<T> CallAsync<T>(Func<IFrankfurterApi, Task<T>> call)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<IFrankfurterApi>());
    }
}
