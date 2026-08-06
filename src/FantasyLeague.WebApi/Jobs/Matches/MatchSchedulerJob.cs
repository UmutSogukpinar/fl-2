using FantasyLeague.Application.Services.Leagues;
using Hangfire;

namespace FantasyLeague.WebApi.Jobs.Matches;

public sealed class MatchSchedulerJob(
    ILeagueService leagueService,
    ILogger<MatchSchedulerJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    public async Task ExecuteAsync(CancellationToken cancellation)
    {
        try
        {
            var count = await leagueService.ProcessDueFixturesAsync(
                DateTime.UtcNow,
                cancellation);

            if (count > 0)
            {
                logger.LogInformation(
                    "Completed {FixtureCount} scheduled demo matches.",
                    count);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process scheduled demo matches.");
            throw;
        }
    }
}
