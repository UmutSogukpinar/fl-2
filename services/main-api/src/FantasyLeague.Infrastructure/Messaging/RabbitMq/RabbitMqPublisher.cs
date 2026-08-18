using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

using FantasyLeague.Application.Common.Interfaces.Messaging;
using FantasyLeague.Infrastructure.Configuration;

namespace FantasyLeague.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqPublisher(
    IRabbitMqConnectionProvider connectionProvider,
    IOptionsMonitor<RabbitMqPublisherOptions> publisherOptions)
    : IIntegrationEventPublisher
{
    public async Task PublishAsync<TMessage>(
        string publisherName,
        TMessage message,
        CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publisherName);
        ArgumentNullException.ThrowIfNull(message);

        var options = publisherOptions.Get(publisherName);

        await using var channel =
            await connectionProvider.CreateChannelAsync(cancellation);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString("N"),
            Type = typeof(TMessage).FullName
        };

        await channel.BasicPublishAsync(
            options.ExchangeName,
            options.RoutingKey,
            mandatory: true,
            properties,
            body,
            cancellation);
    }
}
