using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class DraftRepository(AppDbContext dbContext) : IDraftRepository
{
    public async Task<IReadOnlyList<DraftPickResponse>> GetPicksAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return await (
            from pick in dbContext.Set<DraftPickOrder>().AsNoTracking()
            join team in dbContext.Set<FantasyTeam>() on pick.TeamId equals team.Id
            join nbaPlayer in dbContext.Set<NbaPlayer>() on pick.NbaPlayerId equals (Guid?)nbaPlayer.Id into players
            from nbaPlayer in players.DefaultIfEmpty()
            where pick.LeagueId == leagueId
            orderby pick.OverallPick
            select new DraftPickResponse(
                pick.Id,
                team.Id,
                team.Name,
                pick.Round,
                pick.PositionInRound,
                pick.OverallPick,
                pick.NbaPlayerId,
                nbaPlayer == null ? null : nbaPlayer.FirstName + " " + nbaPlayer.LastName,
                pick.PickedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<DraftPickOrder?> GetCurrentTrackedPickAsync(
        Guid leagueId,
        CancellationToken cancellationToken) =>
        dbContext.Set<DraftPickOrder>()
            .Where(pick => pick.LeagueId == leagueId && pick.NbaPlayerId == null)
            .OrderBy(pick => pick.OverallPick)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsPlayerDraftedAsync(
        Guid leagueId,
        Guid nbaPlayerId,
        CancellationToken cancellationToken) =>
        dbContext.Set<DraftPickOrder>().AnyAsync(
            pick => pick.LeagueId == leagueId && pick.NbaPlayerId == nbaPlayerId,
            cancellationToken);

    public Task<bool> NbaPlayerExistsAsync(
        Guid nbaPlayerId,
        CancellationToken cancellation
    )
    {
        return dbContext.Set<NbaPlayer>().AnyAsync(
                player => player.Id == nbaPlayerId,
                cancellation
            );
    }

    public Task<Guid?> GetFirstAvailablePlayerIdAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        return dbContext.Set<NbaPlayer>()
            .Where(player => !dbContext.Set<DraftPickOrder>().Any(
                pick => pick.LeagueId == leagueId &&
                pick.NbaPlayerId == player.Id)
            )
            .OrderBy(player => player.FirstName)
            .ThenBy(player => player.LastName)
            .Select(player => (Guid?)player.Id)
            .FirstOrDefaultAsync(cancellation);
    }

    public Task<FantasyTeam?> GetTeamAsync(
        Guid leagueId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<FantasyTeam>().SingleOrDefaultAsync(
                team => team.LeagueId == leagueId &&
                team.Id == teamId,
                cancellationToken
            );
    }

    public Task AddRosterPlayerAsync(
        FantasyTeamPlayer player,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<FantasyTeamPlayer>().AddAsync(
            player,
            cancellationToken
        ).AsTask();
    }

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
