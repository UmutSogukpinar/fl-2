using FantasyLeague.Notification.Worker.Consumers;

namespace FantasyLeague.Notification.Worker.Extensions;

public static class NotificationConsumerExtensions
{
    public static IServiceCollection AddNotificationConsumers(
        this IServiceCollection services)
    {
        services.AddHostedService<EmailNotificationConsumer>();

        return services;
    }
}
