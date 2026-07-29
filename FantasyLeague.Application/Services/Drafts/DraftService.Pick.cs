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
        CancellationToken cancellationToken = default)
    {
        request.ValidateMakeDraftPickRequest();

        var league = await GetActiveDraftLeagueAsync(
            leagueId, cancellationToken);
        var currentPick = await GetCurrentPickAsync(
            leagueId, cancellationToken);
        await ValidatePickAsync(
            leagueId, currentPick, request, cancellationToken);

        var pickedAt = DateTime.UtcNow;
        await ApplyPickAsync(
            leagueId,
            currentPick,
            request.NbaPlayerId,
            pickedAt,
            cancellationToken);

        var picksBeforeSave = await draftRepository.GetPicksAsync(
            leagueId, cancellationToken);
        await CompleteDraftIfFinalPickAsync(
            league,
            currentPick,
            picksBeforeSave,
            pickedAt,
            cancellationToken);

        if (!await draftRepository.TrySaveChangesAsync(cancellationToken))
        {
            throw new ConflictException(
                "The draft changed while the pick was being submitted. " +
                "Try again.");
        }

        var picks = await draftRepository.GetPicksAsync(
            leagueId, cancellationToken);
        return CreateState(leagueId, league.Status, league.UpdatedAt, picks);
    }

    private async Task<League> GetActiveDraftLeagueAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        var league = await leagueRepository.GetTrackedByIdAsync(
            leagueId, cancellationToken)
            ?? throw new NotFoundException($"League '{leagueId}' was not found.");
        if (league.Status != LeagueStatus.Drafting)
        {
            throw new ConflictException("The league draft is not active.");
        }

        return league;
    }

    private async Task<DraftPickOrder> GetCurrentPickAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return await draftRepository.GetCurrentTrackedPickAsync(
            leagueId, cancellationToken)
            ?? throw new ConflictException("The draft has no remaining picks.");
    }

    private async Task ValidatePickAsync(
        Guid leagueId,
        DraftPickOrder currentPick,
        MakeDraftPickRequest request,
        CancellationToken cancellationToken)
    {
        if (currentPick.TeamId != request.TeamId)
        {
            throw new ConflictException("It is not this team's turn to pick.");
        }

        var team = await draftRepository.GetTeamAsync(
            leagueId, request.TeamId, cancellationToken)
            ?? throw new NotFoundException(
                $"Fantasy team '{request.TeamId}' was not found.");
        if (team.OwnerId != request.OwnerId)
        {
            throw new ForbiddenException(
                "Only the team owner can make this draft pick.");
        }

        if (!await draftRepository.NbaPlayerExistsAsync(
                request.NbaPlayerId, cancellationToken))
        {
            throw new NotFoundException(
                $"NBA player '{request.NbaPlayerId}' was not found.");
        }

        if (await draftRepository.IsPlayerDraftedAsync(
                leagueId, request.NbaPlayerId, cancellationToken))
        {
            throw new ConflictException(
                "The selected NBA player has already been drafted.");
        }
    }
}
