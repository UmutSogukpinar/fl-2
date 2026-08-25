using System.Text.Json;

using FantasyLeague.Notification.Application.IntegrationEvents;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FantasyLeague.Notification.Worker.Consumers;

public sealed partial class EmailNotificationConsumer
{
    private async Task StartConsumingAsync(
        IChannel channel,
        CancellationToken cancellation)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
            await ProcessMessageAsync(channel, eventArgs, cancellation);

        await channel.BasicConsumeAsync(
            queue: _emailConsumerOptions.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellation);
    }

    private async Task ProcessMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellation)
    {
        string? inboxMessageId = null;
        var inboxProcessingStarted = false;

        try
        {
            inboxMessageId = GetRequiredMessageId(eventArgs);
            inboxProcessingStarted = await TryStartInboxProcessingAsync(
                eventArgs,
                inboxMessageId,
                cancellation);

            if (!inboxProcessingStarted)
            {
                await AcknowledgeDuplicateMessageAsync(
                    channel,
                    eventArgs,
                    inboxMessageId,
                    cancellation);
                return;
            }

            var notification = JsonSerializer
                .Deserialize<EmailNotificationRequested>(eventArgs.Body.Span)
                ?? throw new JsonException(
                    "Email notification message body is null.");

            await _notificationHandler.HandleAsync(
                notification,
                cancellation);

            await CompleteInboxProcessingAsync(
                channel,
                eventArgs,
                inboxMessageId,
                cancellation);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // The unacknowledged message will return to the queue when the
            // channel is closed during application shutdown.
        }
        catch (Exception exception)
        {
            await MarkInboxProcessingFailedAsync(
                inboxMessageId,
                inboxProcessingStarted,
                exception,
                cancellation);

            LogEmailNotificationFailure(
                exception,
                eventArgs.BasicProperties.MessageId);

            await RouteOrRequeueFailedMessageAsync(
                channel,
                eventArgs,
                cancellation);
        }
    }
}
