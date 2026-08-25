namespace FantasyLeague.Notification.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellation = default);
}