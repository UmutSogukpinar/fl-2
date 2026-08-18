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
}
