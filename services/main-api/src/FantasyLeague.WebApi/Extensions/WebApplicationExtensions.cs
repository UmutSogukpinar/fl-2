using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.Database;
using FantasyLeague.WebApi.Hubs;
using FantasyLeague.WebApi.Jobs.Drafts;
using FantasyLeague.WebApi.Jobs.Matches;
using FantasyLeague.WebApi.Jobs.NbaPlayers;
using FantasyLeague.WebApi.Middleware;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeDevelopmentDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();
        await DevelopmentDataSeeder.SeedAsync(
            context,
            services.GetRequiredService<IPasswordHasher>());
    }

    public static WebApplication ConfigureRequestPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<RequestLoggingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.UseHangfireDashboard("/hangfire");
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication ConfigureRecurringJobs(this WebApplication app)
    {
        var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobs.AddOrUpdate<DraftSchedulerJob>(
            "draft-scheduler",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Minutely);
        recurringJobs.AddOrUpdate<MatchSchedulerJob>(
            "match-scheduler",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Minutely);
        recurringJobs.AddOrUpdate<NbaPlayerSyncJob>(
            "nba-player-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(1),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")
            });

        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<FantasyLeagueHub>("/hubs/fantasy");
        app.MapGet("/health", () => "Fantasy League API is running!")
            .AllowAnonymous();

        return app;
    }
}
