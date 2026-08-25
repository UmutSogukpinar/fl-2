using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace FantasyLeague.Notification.Application.Services;

public sealed partial class EmailNotificationHandler(
    ILogger<EmailNotificationHandler> logger)
    : IEmailNotificationHandler
{
    public Task HandleAsync(
        EmailNotificationRequested notification,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellation.ThrowIfCancellationRequested();

        LogEmailNotification(
            logger,
            notification.Recipient,
            notification.Subject,
            notification.Body,
            notification.CorrelationId);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email notification received. Recipient: {Recipient}, " +
            "Subject: {Subject}, Body: {Body}, CorrelationId: {CorrelationId}")]
    private static partial void LogEmailNotification(
        ILogger logger,
        string recipient,
        string subject,
        string body,
        Guid correlationId);
}
