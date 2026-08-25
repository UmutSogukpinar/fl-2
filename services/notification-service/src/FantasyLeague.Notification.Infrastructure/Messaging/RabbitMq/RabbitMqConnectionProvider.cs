using FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;

public sealed partial class RabbitMqConnectionProvider(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionProvider> _logger)
    : IRabbitMqConnectionProvider, IAsyncDisposable
{
    private const int InitialRetryDelaySeconds = 2;
    private const int MaximumRetryDelaySeconds = 30;

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;
    private int _disposed;


    /// <summary>
    /// Returns the existing open connection if available;
    /// otherwise, creates and returns a new connection.
    /// </summary>
    public async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellation = default)
    {
        ThrowIfDisposed();

        var clientProvidedName = "fantasy-league:notification-service";

        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellation);

        try
        {
            ThrowIfDisposed();

            if (_connection is { IsOpen: true })
                return _connection;

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,

                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,

                ClientProvidedName = clientProvidedName

            };

            var retryCount = 0;
            var retryDelaySeconds = InitialRetryDelaySeconds;

            while (true)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(
                        cancellation);

                    if (retryCount > 0)
                        LogConnectionRecovered(retryCount);

                    return _connection;
                }
                catch (OperationCanceledException)
                    when (cancellation.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    retryCount++;

                    LogConnectionRetry(
                        exception,
                        _options.HostName,
                        _options.Port,
                        _options.VirtualHost,
                        retryCount,
                        retryDelaySeconds);

                    await Task.Delay(
                        TimeSpan.FromSeconds(retryDelaySeconds),
                        cancellation);

                    retryDelaySeconds = Math.Min(
                        retryDelaySeconds * 2,
                        MaximumRetryDelaySeconds);
                }
            }
        }
        finally
        {
            _connectionLock.Release();
        }

    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message =
            "RabbitMQ connection could not be established. " +
            "Host: {HostName}, Port: {Port}, VirtualHost: {VirtualHost}. " +
            "Retry {RetryCount} in {RetryDelaySeconds} seconds.")]
    private partial void LogConnectionRetry(
        Exception exception,
        string hostName,
        int port,
        string virtualHost,
        int retryCount,
        int retryDelaySeconds);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection established after {RetryCount} retries.")]
    private partial void LogConnectionRecovered(int retryCount);

    public async Task<IChannel> CreateChannelAsync(
        CancellationToken cancellation = default)
    {
        IConnection connection =
            await GetConnectionAsync(cancellation);

        return await connection.CreateChannelAsync(
            cancellationToken: cancellation
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _connectionLock.WaitAsync();

        try
        {
            if (_connection is not null)
            {
                if (_connection.IsOpen)
                    await _connection.CloseAsync();

                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposed) == 1,
            this);
    }
}
