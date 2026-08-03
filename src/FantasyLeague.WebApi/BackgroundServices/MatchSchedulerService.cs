using FantasyLeague.Application.Services.Leagues;

namespace FantasyLeague.WebApi.BackgroundServices;

public sealed class MatchSchedulerService(
    IServiceScopeFactory scopeFactory,
    ILogger<MatchSchedulerService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await ProcessDueFixturesAsync(cancellationToken);
        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
            await ProcessDueFixturesAsync(cancellationToken);
    }

    private async Task ProcessDueFixturesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var leagueService = scope.ServiceProvider.GetRequiredService<ILeagueService>();
            var count = await leagueService.ProcessDueFixturesAsync(
                DateTime.UtcNow,
                cancellationToken
            );

            if (count > 0)
                logger.LogInformation(
                    "Completed {FixtureCount} scheduled demo matches.",
                    count
                );
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process scheduled demo matches.");
        }
    }
}
