using FantasyLeague.Notification.Infrastructure.Configuration;

namespace FantasyLeague.Notification.Worker.Extensions;

internal static class MailKitExtensions
{
    public static IServiceCollection AddMailKitOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<MailKitOptions>(
            configuration,
            MailKitOptions.SectionName);

        return services;
    }
}
