using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

using FantasyLeague.Infrastructure.Configuration;

namespace FantasyLeague.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConnectionProvider(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionProvider> logger)
    : IRabbitMqConnectionProvider, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private int _disposed;

    public async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            ThrowIfDisposed();

            if (_connection is { IsOpen: true })
                return _connection;

            if (_connection is not null)
                await _connection.DisposeAsync();

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName = "fantasy-league:main-api"
            };

            _connection = await factory.CreateConnectionAsync(
                cancellationToken);

            return _connection;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "RabbitMQ connection could not be established. " +
                "Host: {HostName}, Port: {Port}, VirtualHost: {VirtualHost}",
                _options.HostName,
                _options.Port,
                _options.VirtualHost);

            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);

        return await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);
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
