namespace CacheShowcase.Modules.Pricing.Client;

/// <summary>The third-party provider failed and there was nothing cached to fall back on. Mapped to <c>502</c>.</summary>
public sealed class ProviderUnavailableException : Exception
{
    public ProviderUnavailableException()
    {
    }

    public ProviderUnavailableException(string message)
        : base(message)
    {
    }

    public ProviderUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
