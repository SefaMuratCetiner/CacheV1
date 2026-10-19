using CacheShowcase.BuildingBlocks.Caching.Keys;

namespace CacheShowcase.BuildingBlocks.Caching.Write.WriteBehind;

/// <summary>The Valkey keys of one write-behind buffer.</summary>
internal sealed class WriteBehindBuffer<TSink>(string name, TimeSpan flushInterval, CacheKeyspace keyspace)
    where TSink : IWriteBehindSink
{
    public string Name { get; } = name;

    public TimeSpan FlushInterval { get; } = flushInterval;

    /// <summary>Hash receiving new increments.</summary>
    public string PendingKey { get; } = keyspace.ResolveInternal("write-behind", name, "pending");

    /// <summary>Hash being flushed. Renamed from <see cref="PendingKey"/> so new increments are not lost meanwhile.</summary>
    public string ProcessingKey { get; } = keyspace.ResolveInternal("write-behind", name, "processing");

    /// <summary>Ensures only one instance flushes at a time.</summary>
    public string LockKey { get; } = keyspace.ResolveInternal("write-behind", name, "lock");
}
