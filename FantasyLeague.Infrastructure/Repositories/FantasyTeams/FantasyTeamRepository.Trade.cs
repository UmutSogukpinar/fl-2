using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;
using Microsoft.EntityFrameworkCore;


namespace FantasyLeague.Infrastructure.Repositories.FantasyTeams;

public sealed partial class FantasyTeamRepository
{

    public async Task ReleaseAPlayerAsync(
        Guid teamId,
        Guid playerId,
        CancellationToken cancellationToken
    )
    {
        var player = await _dbContext.Set<FantasyTeamPlayer>()
            .SingleOrDefaultAsync(
                player =>
                    player.FantasyTeamId == teamId &&
                    player.NbaPlayerId == playerId,
                cancellationToken);

        if (player is null)
        {
            throw new NotFoundException(
                $"NBA player '{playerId}'" +
                $" was not found in fantasy team '{teamId}'."
            );
        }

        _dbContext.Set<FantasyTeamPlayer>().Remove(player);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TradeValidationResult>
    ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
        Guid? homeId = null,
        Guid? awayId = null,
        Guid? playerId = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = TradeValidationResult.None;

        var requestedTeamIds = new[] { homeId, awayId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var existingTeamIds = await _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => requestedTeamIds.Contains(team.Id))
            .Select(team => team.Id)
            .ToListAsync(cancellationToken);

        if (homeId.HasValue && !existingTeamIds.Contains(homeId.Value))
            result |= TradeValidationResult.HomeTeamNotFound;

        if (awayId.HasValue && !existingTeamIds.Contains(awayId.Value))
            result |= TradeValidationResult.AwayTeamNotFound;

        if (playerId.HasValue)
        {
            var playerExists = await _dbContext.Set<NbaPlayer>()
                .AsNoTracking()
                .AnyAsync(
                    player => player.Id == playerId.Value,
                    cancellationToken);

            if (!playerExists)
                result |= TradeValidationResult.PlayerNotFound;
        }

        return result;
    }
}
