using FantasyLeague.Application.Common.Interfaces.Messaging;
using FantasyLeague.Application.IntegrationEvents;
using FantasyLeague.Infrastructure.Configuration;
using FantasyLeague.Infrastructure.Messaging.RabbitMq;

namespace FantasyLeague.WebApi.Extensions;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<RabbitMqOptions>(
            configuration,
            RabbitMqOptions.SectionName);

        AddPublisherOptions(
            services,
            configuration,
            IntegrationEventPublisherNames.EmailNotification);

        services.AddSingleton<
            IRabbitMqConnectionProvider,
            RabbitMqConnectionProvider>();
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqPublisher>();

        return services;
    }

    private static void AddPublisherOptions(
        IServiceCollection services,
        IConfiguration configuration,
        string publisherName)
    {
        var sectionPath =
            $"{RabbitMqPublisherOptions.SectionName}:{publisherName}";

        services
            .AddOptions<RabbitMqPublisherOptions>(publisherName)
            .Bind(configuration.GetRequiredSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
