using System.Text.Json;
using CacheShowcase.BuildingBlocks.Caching.Diagnostics;
using CacheShowcase.BuildingBlocks.Caching.Serialization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheShowcase.BuildingBlocks.Caching.Stores.Distributed;

/// <summary>Serializes entries and treats an unreadable entry as a miss instead of an error.</summary>
internal sealed class CacheEntryCodec(ICacheSerializer serializer, ILogger<CacheEntryCodec> logger)
{
    public RedisValue Encode<T>(T value) => serializer.Serialize(value);

    public bool TryDecode<T>(RedisValue raw, string keyForLogs, out T? value)
    {
        value = default;
        if (raw.IsNull)
        {
            return false;
        }

        try
        {
            value = serializer.Deserialize<T>((byte[])raw!);
            return true;
        }
        catch (JsonException exception)
        {
            // Usually a shape change without a schema-version bump. The entry is reloaded and overwritten.
            CacheLog.DeserializationFailed(logger, exception, keyForLogs);
            return false;
        }
    }
}
