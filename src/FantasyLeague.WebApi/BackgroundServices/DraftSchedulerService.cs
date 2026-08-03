using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.WebApi.BackgroundServices;

public sealed class DraftSchedulerService(
    IServiceScopeFactory scopeFactory,
    IHubContext<FantasyLeagueHub> hubContext,
    ILogger<DraftSchedulerService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        await StartDueDraftsAsync(cancellation);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(cancellation))
        {
            await StartDueDraftsAsync(cancellation);
        }
    }

    private async Task StartDueDraftsAsync(CancellationToken cancellation)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var draftService = scope.ServiceProvider.GetRequiredService<IDraftService>();
            var states = await draftService.StartDueDraftsAsync(
                DateTime.UtcNow, cancellation);

            foreach (var state in states)
            {
                await hubContext.Clients
                    .Group(FantasyLeagueHub.LeagueGroup(state.LeagueId))
                    .SendAsync("DraftStarted", state, cancellation);
                logger.LogInformation(
                    "Draft started automatically for league {LeagueId}.", state.LeagueId);
            }

            var autoPickedStates = await draftService.AutoPickExpiredAsync(
                DateTime.UtcNow, cancellation);
            foreach (var state in autoPickedStates)
            {
                var eventName = state.Status == LeagueStatus.Active
                    ? "DraftCompleted"
                    : "DraftUpdated";
                await hubContext.Clients
                    .Group(FantasyLeagueHub.LeagueGroup(state.LeagueId))
                    .SendAsync(eventName, state, cancellation);
                logger.LogInformation(
                    "An expired draft pick was completed automatically for league {LeagueId}. Completed picks: {CompletedPicks}/{TotalPicks}.",
                    state.LeagueId, state.CompletedPicks, state.TotalPicks);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to start scheduled drafts.");
        }
    }
}
