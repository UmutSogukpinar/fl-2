using FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;
using FantasyLeague.Notification.Worker.Configuration.RabbitMq;

namespace FantasyLeague.Notification.Worker.Extensions;

internal static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMqConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddConnectionOptions(services, configuration);
        AddConsumerOptions(services, configuration);

        return services;
    }

    private static void AddConnectionOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetRequiredSection(
                RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void AddConsumerOptions(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        AddConsumerOptions(
            services,
            configuration,
            RabbitMqConsumerNames.Email
        );
    }

    private static void AddConsumerOptions(
        IServiceCollection services,
        IConfiguration configuration,
        string consumerName)
    {
        var consumerSection = configuration.GetRequiredSection(
            $"{RabbitMqConsumerOptions.SectionName}:{consumerName}");

        services
            .AddOptions<RabbitMqConsumerOptions>(consumerName)
            .Bind(consumerSection)
            .ValidateDataAnnotations()
            .Validate(
                options => options.Bindings is { Count: > 0 } &&
                    options.Bindings.All(binding =>
                        binding is not null &&
                        !string.IsNullOrWhiteSpace(binding.ExchangeName) &&
                        !string.IsNullOrWhiteSpace(binding.RoutingKey)),
                "Every RabbitMQ binding must have" +
                " an exchange name and a routing key."
            )
            .Validate(
                options =>
                    options.Retry is not null &&
                    options.DeadLetter is not null &&
                    !string.IsNullOrWhiteSpace(options.Retry.QueueName) &&
                    !string.IsNullOrWhiteSpace(options.Retry.ExchangeName) &&
                    !string.IsNullOrWhiteSpace(options.Retry.RoutingKey) &&
                    !string.IsNullOrWhiteSpace(
                        options.Retry.ReturnExchangeName) &&
                    !string.IsNullOrWhiteSpace(
                        options.Retry.ReturnRoutingKey) &&
                    !string.IsNullOrWhiteSpace(
                        options.DeadLetter.QueueName) &&
                    !string.IsNullOrWhiteSpace(
                        options.DeadLetter.ExchangeName) &&
                    !string.IsNullOrWhiteSpace(
                        options.DeadLetter.RoutingKey) &&
                    options.Retry.DelayMilliseconds is
                        >= 1000 and <= 86_400_000 &&
                    options.Retry.MaxDeliveryAttempts is >= 1 and <= 100 &&
                    new[]
                    {
                        options.QueueName,
                        options.Retry.QueueName,
                        options.DeadLetter.QueueName
                    }.Distinct(StringComparer.Ordinal).Count() == 3,
                "Main, retry, and dead-letter queue settings must be " +
                "complete and use distinct queue names."
            )
            .ValidateOnStart();
    }
}
