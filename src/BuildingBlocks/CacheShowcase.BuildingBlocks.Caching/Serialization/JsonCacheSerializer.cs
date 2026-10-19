using System.Text.Json;

namespace CacheShowcase.BuildingBlocks.Caching.Serialization;

/// <summary>
/// System.Text.Json with web defaults (camelCase). Readable in <c>valkey-cli</c>, which helps while learning.
/// A cached "not found" is stored as the JSON literal <c>null</c>.
/// </summary>
internal sealed class JsonCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public T? Deserialize<T>(ReadOnlySpan<byte> payload) => JsonSerializer.Deserialize<T>(payload, Options);
}
