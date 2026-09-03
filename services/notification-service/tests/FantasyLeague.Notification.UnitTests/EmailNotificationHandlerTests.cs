using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Application.IntegrationEvents;
using FantasyLeague.Notification.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FantasyLeague.Notification.UnitTests;

public sealed class EmailNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsNotification()
    {
        var sender = new RecordingEmailSender();
        var handler = new EmailNotificationHandler(
            NullLogger<EmailNotificationHandler>.Instance,
            sender);
        var notification = new EmailNotificationRequested(
            "recipient@example.com",
            "Trade request",
            "A new trade request is waiting.",
            Guid.NewGuid());

        await handler.HandleAsync(notification);

        Assert.Equal(notification.Recipient, sender.Recipient);
        Assert.Equal(notification.Subject, sender.Subject);
        Assert.Equal(notification.Body, sender.Body);
    }

    [Fact]
    public async Task HandleAsync_WhenSenderFails_PropagatesException()
    {
        var expected = new InvalidOperationException("SMTP unavailable.");
        var handler = new EmailNotificationHandler(
            NullLogger<EmailNotificationHandler>.Instance,
            new FailingEmailSender(expected));
        var notification = new EmailNotificationRequested(
            "recipient@example.com",
            "Trade request",
            "A new trade request is waiting.",
            Guid.NewGuid());

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(notification));

        Assert.Same(expected, actual);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }

        public Task SendAsync(
            string recipient,
            string subject,
            string body,
            CancellationToken cancellation = default)
        {
            Recipient = recipient;
            Subject = subject;
            Body = body;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEmailSender(Exception exception)
        : IEmailSender
    {
        public Task SendAsync(
            string recipient,
            string subject,
            string body,
            CancellationToken cancellation = default)
        {
            return Task.FromException(exception);
        }
    }
}
