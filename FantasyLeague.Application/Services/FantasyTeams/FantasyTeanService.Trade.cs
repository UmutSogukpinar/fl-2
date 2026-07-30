using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed partial class FantasyTeamService
{
    public async Task ReleaseAPlayerAsync(
        Guid id, Guid playerId,
        CancellationToken cancellation = default
    )
    {

        // TODO: Add validation for not going
        // under of half of roster size

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
