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

    [Required]
    public RabbitMqRetryOptions Retry { get; init; } = new();

    [Required]
    public RabbitMqDeadLetterOptions DeadLetter { get; init; } = new();
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

public sealed class RabbitMqRetryOptions
{
    [Required]
    public string QueueName { get; init; } = string.Empty;

    [Required]
    public string ExchangeName { get; init; } = string.Empty;

    [Required]
    public string RoutingKey { get; init; } = string.Empty;

    [Required]
    public string ReturnExchangeName { get; init; } = string.Empty;

    [Required]
    public string ReturnRoutingKey { get; init; } = string.Empty;

    [Range(1000, 86_400_000)]
    public int DelayMilliseconds { get; init; } = 30_000;

    [Range(1, 100)]
    public int MaxDeliveryAttempts { get; init; } = 3;
}

public sealed class RabbitMqDeadLetterOptions
{
    [Required]
    public string QueueName { get; init; } = string.Empty;

    [Required]
    public string ExchangeName { get; init; } = string.Empty;

    [Required]
    public string RoutingKey { get; init; } = string.Empty;
}
