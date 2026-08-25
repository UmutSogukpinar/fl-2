using FantasyLeague.Notification.Application.Common.Interfaces;
using FantasyLeague.Notification.Infrastructure.Configuration;
using FantasyLeague.Notification.Infrastructure.Persistence;
using FantasyLeague.Notification.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Notification.Worker.Extensions;

internal static class PersistenceExtensions
{
    public static IServiceCollection AddNotificationPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<DbSettingsOptions>(
            configuration,
            DbSettingsOptions.SectionName);

        var dbSettings = configuration.GetRequiredOptions<DbSettingsOptions>(
            DbSettingsOptions.SectionName);

        services.AddDbContextFactory<NotificationDbContext>(options =>
            options.UseNpgsql(
                dbSettings.ToConnectionString(),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));
        services.AddSingleton<IInboxMessageStore, EfInboxMessageStore>();

        return services;
    }

    public static async Task MigrateNotificationDatabaseAsync(
        this IHost app,
        CancellationToken cancellation = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<
            IDbContextFactory<NotificationDbContext>>();
        await using var context = await contextFactory
            .CreateDbContextAsync(cancellation);

        await context.Database.MigrateAsync(cancellation);
    }
}
