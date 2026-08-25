using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.IntegrationEvents;


namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed partial class FantasyTeamService
{
    public async Task<IReadOnlyCollection<TeamRosterPlayerResponse>> GetRosterPlayersAsync(
        Guid teamId, CancellationToken cancellation = default)
    {
        await GetTrackedTeamOrThrowAsync(teamId, cancellation);

        return await _teamRepository.GetRosterPlayersAsync(teamId, cancellation);
    }

    public async Task<PagedResponse<TeamRosterPlayerResponse>> GetPlayerPoolAsync(
        Guid teamId,
        PaginationRequest request,
        CancellationToken cancellation = default)
    {
        request.ValidatePaginationRequest();
        await GetTrackedTeamOrThrowAsync(teamId, cancellation);

        var (items, totalCount) = await _teamRepository.GetPlayerPoolAsync(
            teamId, request, cancellation);

        return items.CreateResponse(totalCount, request);
    }

    public async Task<IReadOnlyCollection<TransferResponse>> GetTransfersAsync(
        Guid teamId, CancellationToken cancellation = default)
    {
        await GetTrackedTeamOrThrowAsync(teamId, cancellation);

        return await _teamRepository.GetTransfersAsync(teamId, cancellation);
    }

    public async Task<Guid> CreateTransferAsync(
        Guid initiatingTeamId,
        CreateTransferRequest request,
        CancellationToken cancellation = default
    )
    {
        request = request.NormalizeCreateTransferRequest();
        request.ValidateCreateTransferRequest(initiatingTeamId);

        var conflict = await _teamRepository
            .ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                initiatingTeamId, request.CounterpartyTeamId, null, cancellation);

        CheckConflictForFantasyTeamIdAndNbaPlayerId(
            conflict, initiatingTeamId, request.CounterpartyTeamId);

        var initiatingTeam = await GetTrackedTeamOrThrowAsync(
            initiatingTeamId,
            cancellation);
        var counterpartyTeam = await GetTrackedTeamOrThrowAsync(
            request.CounterpartyTeamId,
            cancellation);
        var recipient = await _userRepository.GetTrackedByIdAsync(
            counterpartyTeam.OwnerId,
            cancellation)
            ?? throw new NotFoundException(
                $"User '{counterpartyTeam.OwnerId}' was not found.");

        var transferId = await _teamRepository.CreateTransferAsync(
            initiatingTeamId,
            request.CounterpartyTeamId,
            request.OfferedPlayerIds,
            request.RequestedPlayerIds,
            cancellation);
        await _teamRepository.SaveChangesAsync(cancellation);

        await _eventPublisher.PublishAsync(
            IntegrationEventPublisherNames.EmailNotification,
            new EmailNotificationRequested(
                recipient.Email,
                "New fantasy trade request",
                $"{initiatingTeam.Name} sent a trade request to " +
                $"{counterpartyTeam.Name}.",
                transferId),
            cancellation);

        return transferId;
    }

    public async Task ApproveTransferAsync(
        Guid transferId, Guid approvingTeamId,
        CancellationToken cancellation = default)
    {
        TransferValidation.ValidateApproveTransferRequest(
            transferId, approvingTeamId);

        await _teamRepository.ApproveTransferAsync(
            transferId, approvingTeamId, cancellation);
        await _teamRepository.SaveChangesAsync(cancellation);
    }

    public async Task ReleaseAPlayerAsync(
        Guid id, Guid playerId,
        CancellationToken cancellation = default
    )
    {
        TransferValidation.ValidateReleasePlayerRequest(id, playerId);

        var conflict = await _teamRepository
            .ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                id, null, playerId, cancellation
        );

        CheckConflictForFantasyTeamIdAndNbaPlayerId(
            conflict,
            homeTeamId: id,
            awayTeamId: null,
            nbaPlayerId: playerId
        );

        var (playerCount, rosterSize) = await _teamRepository
            .GetRosterStateAsync(id, cancellation);
        var minimumRosterSize = (rosterSize + 1) / 2;

        if (playerCount - 1 < minimumRosterSize)
        {
            throw new ConflictException(
                $"A player cannot be released " +
                $"because the roster must contain " +
                $"at least {minimumRosterSize} players."
            );
        }

        await _teamRepository.ReleaseAPlayerAsync(
                id, playerId, cancellation
            );
        await _teamRepository.SaveChangesAsync(cancellation);
    }

    public async Task AddPlayerFromPoolAsync(
        Guid teamId, Guid playerId,
        CancellationToken cancellation = default
    )
    {
        TransferValidation.ValidateAddPlayerFromPoolRequest(teamId, playerId);

        var conflict = await _teamRepository
            .ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                teamId, null, playerId, cancellation
        );

        CheckConflictForFantasyTeamIdAndNbaPlayerId(
            conflict,
            homeTeamId: teamId,
            awayTeamId: null,
            nbaPlayerId: playerId
        );

        var (playerCount, rosterSize) = await _teamRepository
            .GetRosterStateAsync(teamId, cancellation);
        if (playerCount >= rosterSize)
        {
            throw new ConflictException(
                $"A player cannot be added" +
                $"because the roster limit is {rosterSize}."
            );
        }

        await _teamRepository.AddPlayerFromPoolAsync(
            teamId, playerId, cancellation);
        await _teamRepository.SaveChangesAsync(cancellation);
    }

    // ==================== Validations ====================

    private static void CheckConflictForFantasyTeamIdAndNbaPlayerId(
        TradeValidationResult conflict,
        Guid? homeTeamId = null,
        Guid? awayTeamId = null,
        Guid? nbaPlayerId = null)
    {
        if (conflict == TradeValidationResult.None)
            return;

        var errors = new List<string>();

        if (conflict.HasFlag(TradeValidationResult.HomeTeamNotFound))
        {
            errors.Add(homeTeamId.HasValue
                ? $"Home fantasy team '{homeTeamId}' was not found."
                : "Home fantasy team was not found.");
        }

        if (conflict.HasFlag(TradeValidationResult.AwayTeamNotFound))
        {
            errors.Add(awayTeamId.HasValue
                ? $"Away fantasy team '{awayTeamId}' was not found."
                : "Away fantasy team was not found.");
        }

        if (conflict.HasFlag(TradeValidationResult.PlayerNotFound))
        {
            errors.Add(nbaPlayerId.HasValue
                ? $"NBA player '{nbaPlayerId}' was not found."
                : "NBA player was not found.");
        }

        throw new NotFoundException(string.Join(" ", errors));
    }
}
