using FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConnectionProvider(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionProvider> _logger)
    : IRabbitMqConnectionProvider, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;


    /// <summary>
    /// Returns the existing open connection if available;
    /// otherwise, creates and returns a new connection.
    /// </summary>
    public async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellation = default)
    {
        var clientProvidedName = "fantasy-league:notification-service";

        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellation);

        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

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

            _connection = await factory.CreateConnectionAsync(
                cancellation);

            return _connection;
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "RabbitMQ connection could not be established." +
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
        if (_connection is not null)
        {
            if (_connection.IsOpen)
                await _connection.CloseAsync();

            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
