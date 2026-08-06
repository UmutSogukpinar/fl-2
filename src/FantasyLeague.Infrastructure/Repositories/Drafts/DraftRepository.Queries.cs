using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Application.DTOs.Responses.Drafts;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.Drafts;

public sealed partial class DraftRepository
{
    public async Task<IReadOnlyList<DraftPickResponse>> GetPicksAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return await (
            from pick in dbContext.Set<DraftPickOrder>().AsNoTracking()
            join team in dbContext.Set<FantasyTeam>() on pick.TeamId equals team.Id
            join player in dbContext.Set<NbaPlayer>()
                on pick.NbaPlayerId equals (Guid?)player.Id into players
            from player in players.DefaultIfEmpty()
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
                player == null ? null : player.FirstName + " " + player.LastName,
                pick.PickedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<DraftPickOrder?> GetCurrentTrackedPickAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<DraftPickOrder>()
            .Where(pick => pick.LeagueId == leagueId)
            .Where(pick => pick.NbaPlayerId == null)
            .OrderBy(pick => pick.OverallPick)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsPlayerUnavailableAsync(
        Guid leagueId,
        Guid nbaPlayerId,
        CancellationToken cancellationToken)
    {
        var isDrafted = await dbContext.Set<DraftPickOrder>()
            .Where(pick => pick.LeagueId == leagueId)
            .AnyAsync(
                pick => pick.NbaPlayerId == nbaPlayerId,
                cancellationToken);

        if (isDrafted)
        {
            return true;
        }

        return await dbContext.Set<FantasyTeamPlayer>()
            .Where(player => player.LeagueId == leagueId)
            .AnyAsync(
                player => player.NbaPlayerId == nbaPlayerId,
                cancellationToken);
    }

    public Task<bool> NbaPlayerExistsAsync(
        Guid nbaPlayerId,
        CancellationToken cancellation)
    {
        return dbContext.Set<NbaPlayer>().AnyAsync(
            player => player.Id == nbaPlayerId,
            cancellation);
    }

    public Task<Guid?> GetFirstAvailablePlayerIdAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        return dbContext.Set<NbaPlayer>()
            .Where(player => !dbContext.Set<DraftPickOrder>()
                .Where(pick => pick.LeagueId == leagueId)
                .Any(pick => pick.NbaPlayerId == player.Id))
            .Where(player => !dbContext.Set<FantasyTeamPlayer>()
                .Where(rosterPlayer => rosterPlayer.LeagueId == leagueId)
                .Any(rosterPlayer => rosterPlayer.NbaPlayerId == player.Id))
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
        return dbContext.Set<FantasyTeam>()
            .Where(team => team.LeagueId == leagueId)
            .SingleOrDefaultAsync(
                team => team.Id == teamId,
                cancellationToken);
    }
}
