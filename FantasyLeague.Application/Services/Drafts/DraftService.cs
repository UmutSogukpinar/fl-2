using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Drafts;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Application.Common.Validation;

namespace FantasyLeague.Application.Services.Drafts;

public sealed class DraftService(
    ILeagueRepository leagueRepository,
    IFantasyTeamRepository teamRepository,
    ILeagueSetupRepository leagueSetupRepository,
    IDraftRepository draftRepository) : IDraftService
{
    private const int MinimumTeamCount = 2;
    private static readonly TimeSpan PickDuration = TimeSpan.FromSeconds(60);
    public async Task<DraftStateResponse> GetStateAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default)
    {
        var league = await leagueRepository.GetResponseByIdAsync(leagueId, cancellationToken)
            ?? throw new NotFoundException($"League '{leagueId}' was not found.");
        var picks = await draftRepository.GetPicksAsync(leagueId, cancellationToken);
        return CreateState(leagueId, league.Status, league.UpdatedAt, picks);
    }

    public async Task<IReadOnlyList<DraftStateResponse>> StartDueDraftsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var leagues = await leagueRepository.GetDueForDraftAsync(utcNow, cancellationToken);
        if (leagues.Count == 0) return [];

        var startedLeagues = new List<League>();
        foreach (var league in leagues)
        {
            var teamIds = await teamRepository.GetIdsByLeagueIdAsync(
                league.Id, cancellationToken);
            if (teamIds.Count < MinimumTeamCount)
            {
                if (league.Status != LeagueStatus.DraftDelayed)
                {
                    league.Status = LeagueStatus.DraftDelayed;
                    league.UpdatedAt = utcNow;
                }
                continue;
            }

            if (!await leagueSetupRepository.ExistsAsync(league.Id, cancellationToken))
            {
                var randomOrder = LeagueSetupGenerator.CreateRandomTeamOrder(teamIds);
                await leagueSetupRepository.AddAsync(
                    LeagueSetupGenerator.CreateDoubleRoundRobinFixtures(league.Id, randomOrder),
                    LeagueSetupGenerator.CreateSnakeDraftOrder(
                        league.Id, randomOrder, league.Settings.RosterSize),
                    cancellationToken);
            }

            league.Status = LeagueStatus.Drafting;
            league.UpdatedAt = utcNow;
            startedLeagues.Add(league);
        }
        await leagueRepository.SaveChangesAsync(cancellationToken);

        var states = new List<DraftStateResponse>(startedLeagues.Count);
        foreach (var league in startedLeagues)
        {
            var picks = await draftRepository.GetPicksAsync(league.Id, cancellationToken);
            states.Add(CreateState(league.Id, league.Status, league.UpdatedAt, picks));
        }
        return states;
    }

    public async Task<IReadOnlyList<DraftStateResponse>> AutoPickExpiredAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var leagues = await leagueRepository.GetDraftingAsync(cancellationToken);
        var updatedStates = new List<DraftStateResponse>();

        foreach (var league in leagues)
        {
            var picks = await draftRepository.GetPicksAsync(league.Id, cancellationToken);
            var currentPick = picks.FirstOrDefault(pick => !pick.NbaPlayerId.HasValue);
            var deadlineUtc = GetPickDeadlineUtc(currentPick, league.UpdatedAt, picks);
            if (currentPick is null || deadlineUtc is null || deadlineUtc > utcNow)
            {
                continue;
            }

            var trackedPick = await draftRepository.GetCurrentTrackedPickAsync(
                league.Id, cancellationToken);
            var nbaPlayerId = await draftRepository.GetFirstAvailablePlayerIdAsync(
                league.Id, cancellationToken);
            if (trackedPick is null || trackedPick.Id != currentPick.Id || nbaPlayerId is null)
            {
                continue;
            }

            trackedPick.NbaPlayerId = nbaPlayerId.Value;
            trackedPick.PickedAt = utcNow;
            await draftRepository.AddRosterPlayerAsync(new FantasyTeamPlayer
            {
                LeagueId = league.Id,
                FantasyTeamId = trackedPick.TeamId,
                NbaPlayerId = nbaPlayerId.Value,
                AcquiredAt = utcNow
            }, cancellationToken);

            if (trackedPick.OverallPick == picks.Count)
            {
                league.Status = LeagueStatus.Active;
                league.UpdatedAt = utcNow;
            }

            if (!await draftRepository.TrySaveChangesAsync(cancellationToken))
            {
                continue;
            }

            var updatedPicks = await draftRepository.GetPicksAsync(
                league.Id, cancellationToken);
            updatedStates.Add(CreateState(
                league.Id, league.Status, league.UpdatedAt, updatedPicks));
        }

        return updatedStates;
    }

    public async Task<DraftStateResponse> CloseDelayedLeagueAsync(
        Guid leagueId,
        Guid commissionerId,
        CancellationToken cancellationToken = default)
    {
        var league = await leagueRepository.GetTrackedByIdAsync(leagueId, cancellationToken)
            ?? throw new NotFoundException($"League '{leagueId}' was not found.");
        if (league.CommissionerId != commissionerId)
            throw new ForbiddenException("Only the league commissioner can close the league.");
        if (league.Status != LeagueStatus.DraftDelayed)
            throw new ConflictException("Only a delayed league can be closed.");

        league.Status = LeagueStatus.Completed;
        league.UpdatedAt = DateTime.UtcNow;
        await leagueRepository.SaveChangesAsync(cancellationToken);

        var picks = await draftRepository.GetPicksAsync(leagueId, cancellationToken);
        return CreateState(leagueId, league.Status, league.UpdatedAt, picks);
    }

    public async Task<DraftStateResponse> MakePickAsync(
        Guid leagueId,
        MakeDraftPickRequest request,
        CancellationToken cancellationToken = default)
    {
        request.ValidateMakeDraftPickRequest();

        var league = await leagueRepository.GetTrackedByIdAsync(leagueId, cancellationToken)
            ?? throw new NotFoundException($"League '{leagueId}' was not found.");
        if (league.Status != LeagueStatus.Drafting)
            throw new ConflictException("The league draft is not active.");

        var currentPick = await draftRepository.GetCurrentTrackedPickAsync(
            leagueId, cancellationToken)
            ?? throw new ConflictException("The draft has no remaining picks.");
        if (currentPick.TeamId != request.TeamId)
            throw new ConflictException("It is not this team's turn to pick.");

        var team = await draftRepository.GetTeamAsync(
            leagueId, request.TeamId, cancellationToken)
            ?? throw new NotFoundException($"Fantasy team '{request.TeamId}' was not found.");
        if (team.OwnerId != request.OwnerId)
            throw new ForbiddenException("Only the team owner can make this draft pick.");
        if (!await draftRepository.NbaPlayerExistsAsync(request.NbaPlayerId, cancellationToken))
            throw new NotFoundException($"NBA player '{request.NbaPlayerId}' was not found.");
        if (await draftRepository.IsPlayerDraftedAsync(
                leagueId, request.NbaPlayerId, cancellationToken))
            throw new ConflictException("The selected NBA player has already been drafted.");

        var pickedAt = DateTime.UtcNow;
        currentPick.NbaPlayerId = request.NbaPlayerId;
        currentPick.PickedAt = pickedAt;
        await draftRepository.AddRosterPlayerAsync(new FantasyTeamPlayer
        {
            LeagueId = leagueId,
            FantasyTeamId = request.TeamId,
            NbaPlayerId = request.NbaPlayerId,
            AcquiredAt = pickedAt
        }, cancellationToken);

        var picksBeforeSave = await draftRepository.GetPicksAsync(leagueId, cancellationToken);
        if (currentPick.OverallPick == picksBeforeSave.Count)
        {
            league.Status = LeagueStatus.Active;
            league.UpdatedAt = pickedAt;
        }

        if (!await draftRepository.TrySaveChangesAsync(cancellationToken))
            throw new ConflictException("The draft changed while the pick was being submitted. Try again.");

        var picks = await draftRepository.GetPicksAsync(leagueId, cancellationToken);
        return CreateState(leagueId, league.Status, league.UpdatedAt, picks);
    }

    private static DraftStateResponse CreateState(
        Guid leagueId,
        LeagueStatus status,
        DateTime? draftStartedAtUtc,
        IReadOnlyList<DraftPickResponse> picks)
    {
        var completed = picks.Count(pick => pick.NbaPlayerId.HasValue);
        var currentPick = picks.FirstOrDefault(pick => !pick.NbaPlayerId.HasValue);
        var pickDeadlineUtc = GetPickDeadlineUtc(currentPick, draftStartedAtUtc, picks);

        return new DraftStateResponse(
            leagueId,
            status,
            completed,
            picks.Count,
            currentPick,
            pickDeadlineUtc,
            picks);
    }

    private static DateTime? GetPickDeadlineUtc(
        DraftPickResponse? currentPick,
        DateTime? draftStartedAtUtc,
        IReadOnlyList<DraftPickResponse> picks)
    {
        if (currentPick is null) return null;

        var currentPickStartedAtUtc = picks
            .Where(pick => pick.OverallPick < currentPick.OverallPick && pick.PickedAt.HasValue)
            .OrderByDescending(pick => pick.OverallPick)
            .Select(pick => pick.PickedAt)
            .FirstOrDefault() ?? draftStartedAtUtc;

        return currentPickStartedAtUtc?.Add(PickDuration);
    }
}
