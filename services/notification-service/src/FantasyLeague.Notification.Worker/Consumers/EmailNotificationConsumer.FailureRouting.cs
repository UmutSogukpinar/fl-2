using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FantasyLeague.Notification.Worker.Consumers;

public sealed partial class EmailNotificationConsumer
{
    private const string RetryCountHeader = "x-retry-count";

    private async Task RouteOrRequeueFailedMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellation)
    {
        try
        {
            await RouteFailedMessageAsync(channel, eventArgs, cancellation);
        }
        catch (OperationCanceledException)
            when (cancellation.IsCancellationRequested)
        {
            // The original message remains unacknowledged and returns to the
            // queue when the channel closes during shutdown.
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

    private async Task RouteFailedMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellation)
    {
        var deliveryAttempt = GetRetryCount(eventArgs.BasicProperties) + 1;
        var properties = CopyPropertiesWithRetryCount(
            eventArgs.BasicProperties,
            deliveryAttempt);

        if (deliveryAttempt >=
            _emailConsumerOptions.Retry.MaxDeliveryAttempts)
        {
            var deadLetter = _emailConsumerOptions.DeadLetter;

            await channel.BasicPublishAsync(
                exchange: deadLetter.ExchangeName,
                routingKey: deadLetter.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: eventArgs.Body,
                cancellationToken: cancellation);

            LogMessageDeadLettered(
                eventArgs.BasicProperties.MessageId,
                deliveryAttempt,
                deadLetter.QueueName);
        }
        else
        {
            var retry = _emailConsumerOptions.Retry;

            await channel.BasicPublishAsync(
                exchange: retry.ExchangeName,
                routingKey: retry.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: eventArgs.Body,
                cancellationToken: cancellation);

            LogMessageScheduledForRetry(
                eventArgs.BasicProperties.MessageId,
                deliveryAttempt,
                retry.MaxDeliveryAttempts,
                retry.DelayMilliseconds);
        }

        await channel.BasicAckAsync(
            eventArgs.DeliveryTag,
            multiple: false,
            cancellationToken: cancellation);
    }

    private static int GetRetryCount(
        IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null ||
            !properties.Headers.TryGetValue(
                RetryCountHeader,
                out var retryCount) ||
            retryCount is null)
        {
            return 0;
        }

        return retryCount switch
        {
            byte value => value,
            short value => value,
            int value => value,
            long value when value <= int.MaxValue => (int)value,
            _ => 0
        };
    }

    private static BasicProperties CopyPropertiesWithRetryCount(
        IReadOnlyBasicProperties source,
        int retryCount)
    {
        var headers = source.Headers is null
            ? []
            : new Dictionary<string, object?>(source.Headers);

        headers[RetryCountHeader] = retryCount;

        return new BasicProperties
        {
            AppId = source.AppId,
            ClusterId = source.ClusterId,
            ContentEncoding = source.ContentEncoding,
            ContentType = source.ContentType,
            CorrelationId = source.CorrelationId,
            DeliveryMode = source.DeliveryMode,
            Headers = headers,
            MessageId = source.MessageId,
            Priority = source.Priority,
            ReplyTo = source.ReplyTo,
            Timestamp = source.Timestamp,
            Type = source.Type,
            UserId = source.UserId
        };
    }
}
