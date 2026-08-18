using RabbitMQ.Client;

namespace FantasyLeague.Infrastructure.Messaging.RabbitMq;

public interface IRabbitMqConnectionProvider
{
    Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default);

    Task<IChannel> CreateChannelAsync(
        CancellationToken cancellationToken = default);
}
