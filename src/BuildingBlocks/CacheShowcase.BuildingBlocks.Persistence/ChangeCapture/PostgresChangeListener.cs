using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CacheShowcase.BuildingBlocks.Persistence.ChangeCapture;

/// <summary>
/// Change capture for cache invalidation: a database trigger calls <c>pg_notify</c> on every change, and this
/// listener receives it on a dedicated connection (<c>LISTEN</c>). It sees every write, including ones made outside
/// the application, such as manual SQL or another system writing to the same tables.
/// </summary>
/// <remarks>
/// LISTEN/NOTIFY is a lightweight stand-in for log-based CDC (logical replication, Debezium): notifications sent
/// while the listener is disconnected are lost, so entries must still have a TTL as a backstop.
/// </remarks>
public abstract partial class PostgresChangeListener(NpgsqlDataSource dataSource, ILogger logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    /// <summary>The notification channel the trigger publishes to. Must be a plain SQL identifier.</summary>
    protected abstract string Channel { get; }

    /// <summary>Handles one notification payload.</summary>
    protected abstract Task HandleAsync(string payload, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // The listener must survive any failure and reconnect.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogListenerFailed(exception, Channel);
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(stoppingToken);

        // Notifications are raised during WaitAsync; collect them and handle them after it returns.
        var received = new ConcurrentQueue<string>();
        connection.Notification += (_, notification) => received.Enqueue(notification.Payload);

        await using (var listen = new NpgsqlCommand($"LISTEN {Channel}", connection))
        {
            await listen.ExecuteNonQueryAsync(stoppingToken);
        }

        LogListening(Channel);

        while (!stoppingToken.IsCancellationRequested)
        {
            await connection.WaitAsync(stoppingToken);
            while (received.TryDequeue(out var payload))
            {
                await HandleAsync(payload, stoppingToken);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Listening for database changes on channel {Channel}")]
    private partial void LogListening(string channel);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Change listener for {Channel} failed; reconnecting")]
    private partial void LogListenerFailed(Exception exception, string channel);
}
