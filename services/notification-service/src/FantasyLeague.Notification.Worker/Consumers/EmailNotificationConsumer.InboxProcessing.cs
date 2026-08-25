using System.Text;

using FantasyLeague.Notification.Application.IntegrationEvents;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FantasyLeague.Notification.Worker.Consumers;

public sealed partial class EmailNotificationConsumer
{
    private static string GetRequiredMessageId(
        BasicDeliverEventArgs eventArgs)
    {
        var messageId = eventArgs.BasicProperties.MessageId;
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return messageId;
    }

    private Task<bool> TryStartInboxProcessingAsync(
        BasicDeliverEventArgs eventArgs,
        string messageId,
        CancellationToken cancellation)
    {
        var payload = Encoding.UTF8.GetString(eventArgs.Body.Span);
        var messageType = eventArgs.BasicProperties.Type ??
            typeof(EmailNotificationRequested).FullName!;

        return _inboxMessageStore.TryStartProcessingAsync(
            messageId,
            messageType,
            payload,
            cancellation);
    }

    private async Task AcknowledgeDuplicateMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        string messageId,
        CancellationToken cancellation)
    {
        LogDuplicateMessage(messageId);

        await AcknowledgeMessageAsync(channel, eventArgs, cancellation);
    }

    private async Task CompleteInboxProcessingAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        string messageId,
        CancellationToken cancellation)
    {
        await _inboxMessageStore.MarkProcessedAsync(
            messageId,
            cancellation);

        await AcknowledgeMessageAsync(channel, eventArgs, cancellation);
    }

    private async Task MarkInboxProcessingFailedAsync(
        string? messageId,
        bool processingStarted,
        Exception exception,
        CancellationToken cancellation)
    {
        if (!processingStarted || messageId is null)
            return;

        try
        {
            await _inboxMessageStore.MarkFailedAsync(
                messageId,
                exception.ToString(),
                cancellation);
        }
        catch (Exception persistenceException)
        {
            LogInboxFailurePersistenceError(
                persistenceException,
                messageId);
        }
    }

    private static Task AcknowledgeMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellation)
    {
        return channel.BasicAckAsync(
            eventArgs.DeliveryTag,
            multiple: false,
            cancellationToken: cancellation).AsTask();
    }
}
