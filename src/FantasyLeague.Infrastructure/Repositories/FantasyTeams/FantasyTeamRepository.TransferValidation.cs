using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.FantasyTeams;

public sealed partial class FantasyTeamRepository
{
    private async Task ValidateTransferAsync(
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        IReadOnlyCollection<Guid> offeredPlayerIds,
        IReadOnlyCollection<Guid> requestedPlayerIds,
        CancellationToken cancellation)
    {
        var leagueId = await GetSharedLeagueIdAsync(
            initiatingTeamId, counterpartyTeamId, cancellation);
        var rosterPlayers = await GetRosterPlayersAsync(
            initiatingTeamId, counterpartyTeamId, cancellation);

        EnsurePlayersBelongToTeams(
            rosterPlayers, initiatingTeamId, counterpartyTeamId,
            offeredPlayerIds, requestedPlayerIds);
        await EnsureRosterLimitsAsync(
            leagueId, rosterPlayers, initiatingTeamId, counterpartyTeamId,
            offeredPlayerIds.Count, requestedPlayerIds.Count, cancellation);
    }

    private async Task<Guid> GetSharedLeagueIdAsync(
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        CancellationToken cancellation)
    {
        var leagueIds = await _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.Id == initiatingTeamId || team.Id == counterpartyTeamId)
            .Select(team => team.LeagueId)
            .ToArrayAsync(cancellation);

        if (leagueIds.Length != 2)
            throw new NotFoundException("One or more fantasy teams were not found.");
        if (leagueIds[0] != leagueIds[1])
            throw new ConflictException(
                "Players can only be transferred between teams in the same league.");
        return leagueIds[0];
    }

    private Task<FantasyTeamPlayer[]> GetRosterPlayersAsync(
        Guid firstTeamId, Guid secondTeamId, CancellationToken cancellation)
    {
        return _dbContext.Set<FantasyTeamPlayer>()
            .AsNoTracking()
            .Where(player => player.FantasyTeamId == firstTeamId ||
                             player.FantasyTeamId == secondTeamId)
            .ToArrayAsync(cancellation);
    }

    private static void EnsurePlayersBelongToTeams(
        IReadOnlyCollection<FantasyTeamPlayer> rosterPlayers,
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        IEnumerable<Guid> offeredPlayerIds,
        IEnumerable<Guid> requestedPlayerIds)
    {
        var offeredPlayersExist = PlayersBelongToTeam(
            rosterPlayers, initiatingTeamId, offeredPlayerIds);
        var requestedPlayersExist = PlayersBelongToTeam(
            rosterPlayers, counterpartyTeamId, requestedPlayerIds);

        if (!offeredPlayersExist || !requestedPlayersExist)
            throw new NotFoundException(
                "One or more players are not in the specified fantasy team.");
    }

    private static bool PlayersBelongToTeam(
        IEnumerable<FantasyTeamPlayer> rosterPlayers,
        Guid teamId,
        IEnumerable<Guid> playerIds)
    {
        return playerIds.All(playerId => rosterPlayers.Any(
            player => player.FantasyTeamId == teamId &&
                      player.NbaPlayerId == playerId));
    }

    private async Task EnsureRosterLimitsAsync(
        Guid leagueId,
        IReadOnlyCollection<FantasyTeamPlayer> rosterPlayers,
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        int offeredCount,
        int requestedCount,
        CancellationToken cancellation)
    {
        var rosterSize = await GetRosterSizeAsync(leagueId, cancellation);
        var initiatingCount = CountAfterTransfer(
            rosterPlayers, initiatingTeamId, offeredCount, requestedCount);
        var counterpartyCount = CountAfterTransfer(
            rosterPlayers, counterpartyTeamId, requestedCount, offeredCount);

        if (initiatingCount < rosterSize || counterpartyCount < rosterSize)
            throw new ConflictException(
                $"A transfer cannot reduce either roster below {rosterSize} players.");
    }

    private Task<int> GetRosterSizeAsync(
        Guid leagueId, CancellationToken cancellation)
    {
        return _dbContext.Set<LeagueSettings>()
            .Where(settings => settings.LeagueId == leagueId)
            .Select(settings => settings.RosterSize)
            .SingleAsync(cancellation);
    }

    private static int CountAfterTransfer(
        IEnumerable<FantasyTeamPlayer> players,
        Guid teamId,
        int outgoingCount,
        int incomingCount)
    {
        return players.Count(player => player.FantasyTeamId == teamId)
               - outgoingCount
               + incomingCount;
    }

    public async Task<TradeValidationResult> ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
        Guid? homeId = null,
        Guid? awayId = null,
        Guid? playerId = null,
        CancellationToken cancellationToken = default)
    {
        var existingTeamIds = await GetExistingTeamIdsAsync(
            homeId, awayId, cancellationToken);
        var result = GetMissingTeamFlags(homeId, awayId, existingTeamIds);

        if (playerId.HasValue &&
            !await PlayerExistsAsync(playerId.Value, cancellationToken))
            result |= TradeValidationResult.PlayerNotFound;

        return result;
    }

    private async Task<Guid[]> GetExistingTeamIdsAsync(
        Guid? homeId, Guid? awayId, CancellationToken cancellation)
    {
        var requestedIds = new[] { homeId, awayId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        return await _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => requestedIds.Contains(team.Id))
            .Select(team => team.Id)
            .ToArrayAsync(cancellation);
    }

    private static TradeValidationResult GetMissingTeamFlags(
        Guid? homeId, Guid? awayId, IReadOnlyCollection<Guid> existingIds)
    {
        var result = TradeValidationResult.None;
        if (homeId.HasValue && !existingIds.Contains(homeId.Value))
            result |= TradeValidationResult.HomeTeamNotFound;
        if (awayId.HasValue && !existingIds.Contains(awayId.Value))
            result |= TradeValidationResult.AwayTeamNotFound;
        return result;
    }

    private Task<bool> PlayerExistsAsync(
        Guid playerId, CancellationToken cancellation)
    {
        return _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .AnyAsync(player => player.Id == playerId, cancellation);
    }
}
