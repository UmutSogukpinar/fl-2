using FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;

namespace FantasyLeague.Notification.Worker.Consumers;

public sealed class EmailNotificationConsumer
(
    ILogger<EmailNotificationConsumer> _logger,
    IOptionsMonitor<RabbitMqConsumerOptions> consumerOptions) : BackgroundService
{
    private readonly RabbitMqConsumerOptions _options =
        consumerOptions.Get(RabbitMqConsumerNames.Email);

    protected override async Task ExecuteAsync(
        CancellationToken cancellation)
    {
        _logger.LogInformation(
            "Email Notification Consumer is starting for queue {QueueName}.",
            _options.QueueName);

        while (!cancellation.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellation);
        }
    }
}
