using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Infrastructure.Configuration;
using FantasyLeague.Notification.Infrastructure.Email;

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

        services.AddSingleton<IEmailSender, MailKitEmailSender>();

        return services;
    }
}
