using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Worker.Configuration.RabbitMq;
using FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;


namespace FantasyLeague.Notification.Worker.Consumers;

public sealed partial class EmailNotificationConsumer
(
    IRabbitMqConnectionProvider _connProvider,
    IEmailNotificationHandler _notificationHandler,
    IInboxMessageStore _inboxMessageStore,
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

   
    // ====== Logger utils ======

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Processing {QueueName} items")]
    private partial void LogNotificationConsumer(string queueName);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Email notification could not be processed. " +
            "MessageId: {MessageId}")]
    private partial void LogEmailNotificationFailure(
        Exception exception,
        string? messageId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Message {MessageId} scheduled for retry. " +
            "Attempt: {DeliveryAttempt}/{MaxDeliveryAttempts}, " +
            "Delay: {DelayMilliseconds} ms")]
    private partial void LogMessageScheduledForRetry(
        string? messageId,
        int deliveryAttempt,
        int maxDeliveryAttempts,
        int delayMilliseconds);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Message {MessageId} moved to {DeadLetterQueue} after " +
            "{DeliveryAttempt} failed delivery attempts")]
    private partial void LogMessageDeadLettered(
        string? messageId,
        int deliveryAttempt,
        string deadLetterQueue);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed message {MessageId} could not be routed to the " +
            "retry or dead-letter queue")]
    private partial void LogFailedMessageRoutingFailure(
        Exception exception,
        string? messageId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Message {MessageId} was already processed and will be " +
            "acknowledged without invoking the handler")]
    private partial void LogDuplicateMessage(string messageId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "The failure state of inbox message {MessageId} could not " +
            "be persisted")]
    private partial void LogInboxFailurePersistenceError(
        Exception exception,
        string messageId);
}
