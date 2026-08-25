using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Application.Services;
using FantasyLeague.Notification.Worker.Consumers;

namespace FantasyLeague.Notification.Worker.Extensions;

internal static class NotificationConsumerExtensions
{
    public static IServiceCollection AddNotificationConsumers(
        this IServiceCollection services)
    {
        services.AddSingleton<
            IEmailNotificationHandler,
            EmailNotificationHandler>();
        services.AddHostedService<EmailNotificationConsumer>();

        return services;
    }
}
