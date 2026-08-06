using FantasyLeague.Application.Services.NbaPlayers;
using Hangfire;

namespace FantasyLeague.WebApi.Jobs.NbaPlayers;

public sealed class NbaPlayerSyncJob(
    INbaPlayerSyncService syncService,
    ILogger<NbaPlayerSyncJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ExecuteAsync(CancellationToken cancellation)
    {
        try
        {
            var result = await syncService.SyncActivePlayersAsync(cancellation);
            logger.LogInformation(
                "NBA player synchronization completed. " +
                "Processed: {ProcessedCount}, Created: {CreatedCount}, Updated: {UpdatedCount}, " +
                "Statistics: {StatisticsCount}.",
                result.ProcessedCount,
                result.CreatedCount,
                result.UpdatedCount,
                result.StatisticsProcessedCount);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "NBA player synchronization failed.");
            throw;
        }
    }
}
