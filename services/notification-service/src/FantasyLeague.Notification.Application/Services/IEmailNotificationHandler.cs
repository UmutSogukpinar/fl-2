using FantasyLeague.Notification.Application.IntegrationEvents;

namespace FantasyLeague.Notification.Application.Services;

public interface IEmailNotificationHandler
{
    Task HandleAsync(
        EmailNotificationRequested notification,
        CancellationToken cancellation = default);
}
