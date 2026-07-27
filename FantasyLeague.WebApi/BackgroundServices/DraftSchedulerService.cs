using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FantasyLeague.WebApi.BackgroundServices;

public sealed class DraftSchedulerService(
    IServiceScopeFactory scopeFactory,
    IHubContext<FantasyLeagueHub> hubContext,
    ILogger<DraftSchedulerService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartDueDraftsAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await StartDueDraftsAsync(stoppingToken);
        }
    }

    private async Task StartDueDraftsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var draftService = scope.ServiceProvider.GetRequiredService<IDraftService>();
            var states = await draftService.StartDueDraftsAsync(
                DateTime.UtcNow, cancellationToken);

            foreach (var state in states)
            {
                await hubContext.Clients
                    .Group(FantasyLeagueHub.LeagueGroup(state.LeagueId))
                    .SendAsync("DraftStarted", state, cancellationToken);
                logger.LogInformation(
                    "Draft started automatically for league {LeagueId}.", state.LeagueId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to start scheduled drafts.");
        }
    }
}
