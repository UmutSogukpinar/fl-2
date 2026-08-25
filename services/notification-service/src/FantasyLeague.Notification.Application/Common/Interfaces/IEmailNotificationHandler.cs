using FantasyLeague.Notification.Application.IntegrationEvents;

namespace FantasyLeague.Notification.Application.Common.Interfaces;

public interface IEmailNotificationHandler
{
    Task HandleAsync(
        EmailNotificationRequested notification,
        CancellationToken cancellation = default);
}
