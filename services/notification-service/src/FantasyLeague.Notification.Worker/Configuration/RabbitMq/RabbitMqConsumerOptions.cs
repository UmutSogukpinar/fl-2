using System.ComponentModel.DataAnnotations;
using RabbitMqExchangeType = RabbitMQ.Client.ExchangeType;


namespace FantasyLeague.Notification.Worker.Configuration.RabbitMq;

public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMqConsumers";

    [Required]
    public string QueueName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public IReadOnlyList<RabbitMqBindingOptions> Bindings { get; init; } = [];

    [Range(1, 1000)]
    public ushort PrefetchCount { get; init; } = 10;
}

public static class RabbitMqConsumerNames
{
    public const string Email = "Email";
}

public sealed class RabbitMqBindingOptions
{
    [Required]
    public string ExchangeName { get; init; } = string.Empty;

    [Required]
    public string RoutingKey { get; init; } = string.Empty;

    [AllowedValues(
        null,
        RabbitMqExchangeType.Direct,
        RabbitMqExchangeType.Topic,
        RabbitMqExchangeType.Fanout,
        RabbitMqExchangeType.Headers,
        ErrorMessage =
            "ExchangeType must be direct, topic, fanout, or headers.")]
    public string? ExchangeType { get; init; } = string.Empty;
}
