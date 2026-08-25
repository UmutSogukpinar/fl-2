using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace FantasyLeague.Notification.Application.Services;

public sealed partial class EmailNotificationHandler(
    ILogger<EmailNotificationHandler> _logger,
    IEmailSender _emailSender)
    : IEmailNotificationHandler
{
    public async Task HandleAsync(
        EmailNotificationRequested notification,
        CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellation.ThrowIfCancellationRequested();

        LogEmailNotification(
            _logger,
            notification.Recipient,
            notification.Subject,
            notification.CorrelationId);

        await _emailSender.SendAsync(
            notification.Recipient,
            notification.Subject,
            notification.Body,
            cancellation);

        LogEmailNotificationSent(
            _logger,
            notification.Recipient,
            notification.CorrelationId);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email notification received. Recipient: {Recipient}, " +
            "Subject: {Subject}, CorrelationId: {CorrelationId}")]
    private static partial void LogEmailNotification(
        ILogger logger,
        string recipient,
        string subject,
        Guid correlationId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email notification sent. Recipient: {Recipient}, " +
            "CorrelationId: {CorrelationId}")]
    private static partial void LogEmailNotificationSent(
        ILogger logger,
        string recipient,
        Guid correlationId);
}
