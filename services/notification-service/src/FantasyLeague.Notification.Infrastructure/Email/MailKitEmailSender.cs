using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FantasyLeague.Notification.Infrastructure.Email;

public sealed class MailKitEmailSender(
    IOptions<MailKitOptions> options)
    : IEmailSender
{
    private readonly MailKitOptions _options = options.Value;

    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var message = CreateMessage(recipient, subject, body);

        using var client = new SmtpClient();
        client.Timeout = _options.TimeoutMilliseconds;

        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                socketOptions,
                cancellation);
            await client.AuthenticateAsync(
                _options.UserName,
                _options.Password,
                cancellation);
            await client.SendAsync(message, cancellation);
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(
                    true,
                    CancellationToken.None);
        }
    }

    private MimeMessage CreateMessage(
        string recipient,
        string subject,
        string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _options.SenderName,
            _options.SenderEmail));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };

        return message;
    }
}
