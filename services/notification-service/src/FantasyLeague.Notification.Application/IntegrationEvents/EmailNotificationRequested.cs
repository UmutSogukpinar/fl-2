namespace FantasyLeague.Notification.Application.IntegrationEvents;

public sealed record EmailNotificationRequested(
    string Recipient,
    string Subject,
    string Body,
    Guid CorrelationId
);
