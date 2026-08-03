using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Domain.Enums;
using FantasyLeague.WebApi.Hubs;
using Hangfire;
using Microsoft.AspNetCore.SignalR;

namespace FantasyLeague.WebApi.Jobs.Drafts;

public sealed class DraftSchedulerJob(
    IDraftService draftService,
    IHubContext<FantasyLeagueHub> hubContext,
    ILogger<DraftSchedulerJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
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

            var autoPickedStates = await draftService.AutoPickExpiredAsync(
                DateTime.UtcNow, cancellationToken);
            foreach (var state in autoPickedStates)
            {
                var eventName = state.Status == LeagueStatus.Active
                    ? "DraftCompleted"
                    : "DraftUpdated";
                await hubContext.Clients
                    .Group(FantasyLeagueHub.LeagueGroup(state.LeagueId))
                    .SendAsync(eventName, state, cancellationToken);
                logger.LogInformation(
                    "An expired draft pick was completed automatically for league {LeagueId}. Completed picks: {CompletedPicks}/{TotalPicks}.",
                    state.LeagueId, state.CompletedPicks, state.TotalPicks);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process scheduled drafts.");
            throw;
        }
    }
}
