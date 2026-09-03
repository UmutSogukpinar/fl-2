using System.ComponentModel.DataAnnotations;
using FantasyLeague.Notification.Infrastructure.Configuration;

namespace FantasyLeague.Notification.UnitTests;

public sealed class MailKitOptionsTests
{
    [Fact]
    public void Validation_WhenConfigurationIsValid_Succeeds()
    {
        var options = CreateValidOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_WhenTimeoutIsTooSmall_Fails()
    {
        var options = CreateValidOptions(timeoutMilliseconds: 999);

        var results = Validate(options);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(
                nameof(MailKitOptions.TimeoutMilliseconds)));
    }

    private static MailKitOptions CreateValidOptions(
        int timeoutMilliseconds = 30_000)
    {
        return new MailKitOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            SenderEmail = "sender@example.com",
            SenderName = "Fantasy League",
            UserName = "sender@example.com",
            Password = "secret",
            TimeoutMilliseconds = timeoutMilliseconds
        };
    }

    private static IReadOnlyCollection<ValidationResult> Validate(
        MailKitOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);

        return results;
    }
}
