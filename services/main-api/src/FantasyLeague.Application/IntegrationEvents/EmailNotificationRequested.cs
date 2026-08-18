namespace FantasyLeague.Application.IntegrationEvents;

public sealed record EmailNotificationRequested(
    string Recipient,
    string Subject,
    string Body,
    Guid CorrelationId);

public static class IntegrationEventPublisherNames
{
    public const string EmailNotification = "EmailNotification";
}
