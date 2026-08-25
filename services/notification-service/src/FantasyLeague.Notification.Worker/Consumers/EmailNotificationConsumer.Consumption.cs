using System.Text.Json;
using System.Text;

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
            inboxMessageId = eventArgs.BasicProperties.MessageId;
            ArgumentException.ThrowIfNullOrWhiteSpace(inboxMessageId);

            var payload = Encoding.UTF8.GetString(eventArgs.Body.Span);
            var messageType = eventArgs.BasicProperties.Type ??
                typeof(EmailNotificationRequested).FullName!;

            inboxProcessingStarted = await _inboxMessageStore
                .TryStartProcessingAsync(
                    inboxMessageId,
                    messageType,
                    payload,
                    cancellation);

            if (!inboxProcessingStarted)
            {
                LogDuplicateMessage(inboxMessageId);

                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellation);
                return;
            }

            var notification = JsonSerializer
                .Deserialize<EmailNotificationRequested>(eventArgs.Body.Span)
                ?? throw new JsonException(
                    "Email notification message body is null.");

            await _notificationHandler.HandleAsync(
                notification,
                cancellation);

            await _inboxMessageStore.MarkProcessedAsync(
                inboxMessageId,
                cancellation);

            await channel.BasicAckAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellation);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // The unacknowledged message will return to the queue when the
            // channel is closed during application shutdown.
        }
        catch (Exception exception)
        {
            if (inboxProcessingStarted && inboxMessageId is not null)
            {
                try
                {
                    await _inboxMessageStore.MarkFailedAsync(
                        inboxMessageId,
                        exception.ToString(),
                        cancellation);
                }
                catch (Exception persistenceException)
                {
                    LogInboxFailurePersistenceError(
                        persistenceException,
                        inboxMessageId);
                }
            }

            LogEmailNotificationFailure(
                exception,
                eventArgs.BasicProperties.MessageId);

            try
            {
                await RouteFailedMessageAsync(
                    channel,
                    eventArgs,
                    cancellation);
            }
            catch (OperationCanceledException)
                when (cancellation.IsCancellationRequested)
            {
                // The original message remains unacknowledged and returns to
                // the queue when the channel closes during shutdown.
            }
            catch (Exception routingException)
            {
                LogFailedMessageRoutingFailure(
                    routingException,
                    eventArgs.BasicProperties.MessageId);

                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: cancellation);
            }
        }
    }
}
