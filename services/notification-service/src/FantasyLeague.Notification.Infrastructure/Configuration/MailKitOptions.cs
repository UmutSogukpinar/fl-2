using System.ComponentModel.DataAnnotations;

namespace FantasyLeague.Notification.Infrastructure.Configuration;

public sealed class MailKitOptions
{
    public const string SectionName = "MailKit";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    [Required]
    [EmailAddress]
    public string SenderEmail { get; init; } = string.Empty;

    [Required]
    public string SenderName { get; init; } = string.Empty;

    [Required]
    public string UserName { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public bool UseSsl { get; init; }
}
