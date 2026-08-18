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
                "an exchange nameand a routing key."
            )
            .ValidateOnStart();
    }
}
