using FantasyLeague.Notification.Infrastructure.Configuration;
using FantasyLeague.Notification.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace FantasyLeague.Notification.IntegrationTests;

public sealed class MailKitEmailSenderIntegrationTests
{
    [Fact(Skip = "Requires an explicitly configured SMTP test account.")]
    public async Task SendAsync_WithSmtpServer_SendsEmail()
    {
        var options = Options.Create(new MailKitOptions
        {
            Host = GetRequiredEnvironmentVariable("MAILKIT_HOST"),
            Port = int.Parse(GetRequiredEnvironmentVariable("MAILKIT_PORT")),
            SenderEmail = GetRequiredEnvironmentVariable(
                "MAILKIT_SENDER_EMAIL"),
            SenderName = GetRequiredEnvironmentVariable(
                "MAILKIT_SENDER_NAME"),
            UserName = GetRequiredEnvironmentVariable("MAILKIT_USERNAME"),
            Password = GetRequiredEnvironmentVariable("MAILKIT_PASSWORD"),
            UseSsl = bool.Parse(GetRequiredEnvironmentVariable(
                "MAILKIT_USE_SSL")),
            TimeoutMilliseconds = 30_000
        });
        var sender = new MailKitEmailSender(options);

        await sender.SendAsync(
            GetRequiredEnvironmentVariable("MAILKIT_TEST_RECIPIENT"),
            "Fantasy League integration test",
            "MailKit SMTP integration test message.");
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException(
                $"Environment variable '{name}' is required.");
    }
}
