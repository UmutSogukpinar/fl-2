using System.ComponentModel.DataAnnotations;

namespace FantasyLeague.Infrastructure.Configuration;

public sealed class RabbitMqPublisherOptions
{
    public const string SectionName = "RabbitMqPublishers";

    [Required]
    public string ExchangeName { get; init; } = string.Empty;

    [Required]
    public string RoutingKey { get; init; } = string.Empty;
}
