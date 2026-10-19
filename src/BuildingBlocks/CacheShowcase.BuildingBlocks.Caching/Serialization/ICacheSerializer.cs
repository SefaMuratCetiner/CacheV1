namespace CacheShowcase.BuildingBlocks.Caching.Serialization;

/// <summary>Turns cached values into bytes and back. The swap point for a binary format such as MessagePack.</summary>
public interface ICacheSerializer
{
    byte[] Serialize<T>(T value);

    /// <exception cref="System.Text.Json.JsonException">The payload does not match <typeparamref name="T"/>.</exception>
    T? Deserialize<T>(ReadOnlySpan<byte> payload);
}
