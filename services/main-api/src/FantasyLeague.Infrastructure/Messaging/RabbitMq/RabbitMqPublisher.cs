using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

using FantasyLeague.Application.Common.Interfaces.Messaging;
using FantasyLeague.Infrastructure.Configuration;

namespace FantasyLeague.Infrastructure.Messaging.RabbitMq;

public sealed partial class RabbitMqPublisher(
    IRabbitMqConnectionProvider connectionProvider,
    IOptionsMonitor<RabbitMqPublisherOptions> publisherOptions,
    ILogger<RabbitMqPublisher> logger)
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

        try
        {
            await channel.BasicPublishAsync(
                options.ExchangeName,
                options.RoutingKey,
                mandatory: true,
                properties,
                body,
                cancellation);
        }
        catch (PublishException exception)
        {
            LogPublishFailure(
                logger,
                exception,
                publisherName,
                options.ExchangeName,
                options.RoutingKey,
                properties.MessageId,
                exception.IsReturn);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "RabbitMQ message could not be published. " +
            "Publisher: {PublisherName}, Exchange: {ExchangeName}, " +
            "RoutingKey: {RoutingKey}, MessageId: {MessageId}, " +
            "Unroutable: {IsReturn}")]
    private static partial void LogPublishFailure(
        ILogger logger,
        Exception exception,
        string publisherName,
        string exchangeName,
        string routingKey,
        string messageId,
        bool isReturn);
}
