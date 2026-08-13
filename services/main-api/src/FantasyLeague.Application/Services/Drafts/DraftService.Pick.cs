using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Drafts;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    public async Task<DraftStateResponse> MakePickAsync(
        Guid leagueId,
        MakeDraftPickRequest request,
        CancellationToken cancellation = default)
    {
        request.ValidateMakeDraftPickRequest();

        var league = await GetActiveDraftLeagueAsync(
            leagueId, cancellation);
        var currentPick = await GetCurrentPickAsync(
            leagueId, cancellation);
        await ValidatePickAsync(
            leagueId, currentPick, request, cancellation);

        var pickedAt = DateTime.UtcNow;
        await ApplyPickAsync(
            leagueId,
            currentPick,
            request.NbaPlayerId,
            pickedAt,
            cancellation);

        var picksBeforeSave = await draftRepository.GetPicksAsync(
            leagueId, cancellation);
        await CompleteDraftIfFinalPickAsync(
            league,
            currentPick,
            picksBeforeSave,
            pickedAt,
            cancellation);

        ResetFailureCount(league);

        if (!await draftRepository.TrySaveChangesAsync(cancellation))
        {
            var cancellationState = await GetCancellationStateAfterFailureAsync(
                leagueId,
                pickedAt,
                cancellation);

            if (cancellationState is not null)
            {
                return cancellationState;
            }

            throw new ConflictException(
                "A system error prevented the draft pick. The operation will " +
                "be retried; the draft is cancelled after five consecutive failures.");
        }

        var picks = await draftRepository.GetPicksAsync(
            leagueId, cancellation);
        return CreateState(leagueId, league.Status, league.UpdatedAt, picks);
    }

    private async Task<League> GetActiveDraftLeagueAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        var league = await leagueRepository.GetTrackedByIdAsync(
            leagueId, cancellation)
            ?? throw new NotFoundException($"League '{leagueId}' was not found.");
        if (league.Status != LeagueStatus.Drafting)
        {
            throw new ConflictException("The league draft is not active.");
        }

        return league;
    }

    private async Task<DraftPickOrder> GetCurrentPickAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        return await draftRepository.GetCurrentTrackedPickAsync(
            leagueId, cancellation)
            ?? throw new ConflictException("The draft has no remaining picks.");
    }

    private async Task ValidatePickAsync(
        Guid leagueId,
        DraftPickOrder currentPick,
        MakeDraftPickRequest request,
        CancellationToken cancellation)
    {
        if (currentPick.TeamId != request.TeamId)
        {
            throw new ConflictException("It is not this team's turn to pick.");
        }

        var team = await draftRepository.GetTeamAsync(
            leagueId, request.TeamId, cancellation)
            ?? throw new NotFoundException(
                $"Fantasy team '{request.TeamId}' was not found.");
        if (team.OwnerId != request.OwnerId)
        {
            throw new ForbiddenException(
                "Only the team owner can make this draft pick.");
        }

        if (!await draftRepository.NbaPlayerExistsAsync(
                request.NbaPlayerId, cancellation))
        {
            throw new NotFoundException(
                $"NBA player '{request.NbaPlayerId}' was not found.");
        }

        if (await draftRepository.IsPlayerUnavailableAsync(
                leagueId, request.NbaPlayerId, cancellation))
        {
            throw new ConflictException(
                "The selected NBA player is not available for this draft.");
        }
    }
}
