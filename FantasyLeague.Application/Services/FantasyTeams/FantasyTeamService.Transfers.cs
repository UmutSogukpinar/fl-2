using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed partial class FantasyTeamService
{
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

        return await _teamRepository.CreateTransferAsync(
            initiatingTeamId,
            request.CounterpartyTeamId,
            request.OfferedPlayerIds,
            request.RequestedPlayerIds,
            cancellation);
    }

    public Task ApproveTransferAsync(
        Guid transferId, Guid approvingTeamId,
        CancellationToken cancellation = default)
    {
        TransferValidation.ValidateApproveTransferRequest(
            transferId, approvingTeamId);

        return _teamRepository.ApproveTransferAsync(
            transferId, approvingTeamId, cancellation);
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
                $"A player cannot be released because the roster must contain at least {minimumRosterSize} players."
            );
        }

        await _teamRepository.ReleaseAPlayerAsync(
                id, playerId, cancellation
            );
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
