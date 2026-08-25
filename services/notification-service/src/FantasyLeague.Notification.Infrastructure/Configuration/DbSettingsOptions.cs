using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace FantasyLeague.Notification.Infrastructure.Configuration;

public sealed class DbSettingsOptions
{
    public const string SectionName = "DbSettings";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 5432;

    [Required]
    public string Database { get; init; } = string.Empty;

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public string ToConnectionString() => new NpgsqlConnectionStringBuilder
    {
        Host = Host,
        Port = Port,
        Database = Database,
        Username = Username,
        Password = Password
    }.ConnectionString;
}
