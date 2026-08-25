using RabbitMQ.Client;

using FantasyLeague.Notification.Worker.Configuration.RabbitMq;

namespace FantasyLeague.Notification.Worker.Consumers;

public sealed partial class EmailNotificationConsumer
{
    private async Task ConfigureChannelAsync(
        IChannel channel, CancellationToken cancellation)
    {
        await ConfigurePrefetch(channel, cancellation);
        await ConfigureQueueDeclareAsync(channel, cancellation);

        foreach (RabbitMqBindingOptions opt
            in _emailConsumerOptions.Bindings)
        {
            await ConfigureExchangeDeclaration(
                channel,
                opt,
                cancellation
            );

            await ConfigureQueueBinding(
                channel,
                opt,
                cancellation
            );
        }

        await ConfigureRetryTopologyAsync(channel, cancellation);
        await ConfigureDeadLetterTopologyAsync(channel, cancellation);
    }

    private async Task ConfigurePrefetch(
        IChannel channel,
        CancellationToken cancellation)
    {
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _emailConsumerOptions.PrefetchCount,
            global: false,
            cancellationToken: cancellation
        );
    }

    private async Task ConfigureQueueDeclareAsync(
        IChannel channel, CancellationToken cancellation)
    {
        await channel.QueueDeclareAsync(
            queue: _emailConsumerOptions.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellation
        );
    }

    private async Task ConfigureExchangeDeclaration(
        IChannel channel,
        RabbitMqBindingOptions options,
        CancellationToken cancellation)
    {
        await channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: options.ExchangeType ?? "",
            durable: true,
            autoDelete: false,
            cancellationToken: cancellation
        );
    }

    private async Task ConfigureQueueBinding(
        IChannel channel,
        RabbitMqBindingOptions options,
        CancellationToken cancellation)
    {
        await channel.QueueBindAsync(
            queue: _emailConsumerOptions.QueueName,
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey,
            cancellationToken: cancellation
        );
    }

    private async Task ConfigureRetryTopologyAsync(
        IChannel channel,
        CancellationToken cancellation)
    {
        var options = _emailConsumerOptions.Retry;

        await ConfigureDirectExchangeAsync(
            channel,
            options.ExchangeName,
            cancellation);

        var arguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = options.DelayMilliseconds,
            ["x-dead-letter-exchange"] = options.ReturnExchangeName,
            ["x-dead-letter-routing-key"] = options.ReturnRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: cancellation);

        await channel.QueueBindAsync(
            queue: options.QueueName,
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey,
            cancellationToken: cancellation);
    }

    private async Task ConfigureDeadLetterTopologyAsync(
        IChannel channel,
        CancellationToken cancellation)
    {
        var options = _emailConsumerOptions.DeadLetter;

        await ConfigureDirectExchangeAsync(
            channel,
            options.ExchangeName,
            cancellation);

        await channel.QueueDeclareAsync(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellation);

        await channel.QueueBindAsync(
            queue: options.QueueName,
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey,
            cancellationToken: cancellation);
    }

    private static async Task ConfigureDirectExchangeAsync(
        IChannel channel,
        string exchangeName,
        CancellationToken cancellation)
    {
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellation);
    }
}
