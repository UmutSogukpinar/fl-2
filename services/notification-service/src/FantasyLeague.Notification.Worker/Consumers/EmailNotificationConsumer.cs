using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Worker.Configuration.RabbitMq;
using FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;


namespace FantasyLeague.Notification.Worker.Consumers;

public sealed partial class EmailNotificationConsumer
(
    IRabbitMqConnectionProvider _connProvider,
    IEmailNotificationHandler _notificationHandler,
    ILogger<EmailNotificationConsumer> _logger,
    IOptionsMonitor<RabbitMqConsumerOptions> consumerOptions)
    : BackgroundService
{
    private readonly RabbitMqConsumerOptions _emailConsumerOptions =
        consumerOptions.Get(RabbitMqConsumerNames.Email);

    protected override async Task ExecuteAsync(
        CancellationToken cancellation)
    {
        var conn = await _connProvider.GetConnectionAsync(cancellation);

        await using var channel = await conn
            .CreateChannelAsync(cancellationToken: cancellation);

        await ConfigureChannelAsync(channel, cancellation);
        await StartConsumingAsync(channel, cancellation);

        LogNotificationConsumer(_emailConsumerOptions.QueueName);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
    }

   
    // == Logger utils ==

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Processing {QueueName} items")]
    private partial void LogNotificationConsumer(string queueName);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Email notification could not be processed. " +
            "MessageId: {MessageId}")]
    private partial void LogEmailNotificationFailure(
        Exception exception,
        string? messageId);
}
