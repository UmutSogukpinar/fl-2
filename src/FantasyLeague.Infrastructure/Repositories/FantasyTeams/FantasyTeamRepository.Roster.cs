using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Infrastructure.Repositories.FantasyTeams;

public sealed partial class FantasyTeamRepository
{
    public async Task<IReadOnlyCollection<TeamRosterPlayerResponse>> GetRosterPlayersAsync(
        Guid teamId, CancellationToken cancellation)
    {
        return await (
            from roster in _dbContext.Set<FantasyTeamPlayer>().AsNoTracking()
            join player in _dbContext.Set<NbaPlayer>().AsNoTracking()
                on roster.NbaPlayerId equals player.Id
            where roster.FantasyTeamId == teamId
            orderby player.FirstName, player.LastName
            select new TeamRosterPlayerResponse(
                player.Id, player.FirstName, player.LastName,
                player.Team, player.Position))
            .ToArrayAsync(cancellation);
    }

    public async Task<(IReadOnlyCollection<TeamRosterPlayerResponse> Items, int TotalCount)>
        GetPlayerPoolAsync(
            Guid teamId,
            PaginationRequest request,
            CancellationToken cancellation)
    {
        var leagueId = await GetTeamLeagueIdAsync(teamId, cancellation);
        var query = _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(player => !_dbContext.Set<FantasyTeamPlayer>().Any(roster =>
                roster.LeagueId == leagueId && roster.NbaPlayerId == player.Id));
        var totalCount = await query.CountAsync(cancellation);
        var items = await query
            .OrderBy(player => player.FirstName)
            .ThenBy(player => player.LastName)
            .ApplyPagination(request)
            .Select(player => new TeamRosterPlayerResponse(
                player.Id, player.FirstName, player.LastName,
                player.Team, player.Position))
            .ToArrayAsync(cancellation);

        return (items, totalCount);
    }

    public async Task<(int PlayerCount, int RosterSize)> GetRosterStateAsync(
        Guid teamId, CancellationToken cancellation)
    {
        var rosterSize = await (
                from team in _dbContext.Set<FantasyTeam>().AsNoTracking()
                join settings in _dbContext.Set<LeagueSettings>().AsNoTracking()
                    on team.LeagueId equals settings.LeagueId
                where team.Id == teamId
                select settings.RosterSize)
            .SingleAsync(cancellation);

        var playerCount = await _dbContext.Set<FantasyTeamPlayer>()
            .AsNoTracking()
            .CountAsync(player => player.FantasyTeamId == teamId, cancellation);

        return (playerCount, rosterSize);
    }

    public async Task ReleaseAPlayerAsync(
        Guid teamId, Guid playerId, CancellationToken cancellation)
    {
        var player = await _dbContext.Set<FantasyTeamPlayer>()
            .SingleOrDefaultAsync(
                item => item.FantasyTeamId == teamId && item.NbaPlayerId == playerId,
                cancellation);

        if (player is null)
        {
            throw new NotFoundException(
                $"NBA player '{playerId}' was not found in fantasy team '{teamId}'.");
        }

        _dbContext.Set<FantasyTeamPlayer>().Remove(player);
    }

    public async Task AddPlayerFromPoolAsync(
        Guid teamId, Guid playerId, CancellationToken cancellation)
    {
        var leagueId = await GetTeamLeagueIdAsync(teamId, cancellation);
        await EnsurePlayerIsAvailableAsync(leagueId, playerId, cancellation);

        await _dbContext.Set<FantasyTeamPlayer>().AddAsync(
            new FantasyTeamPlayer
            {
                FantasyTeamId = teamId,
                LeagueId = leagueId,
                NbaPlayerId = playerId
            },
            cancellation);
    }

    private Task<Guid> GetTeamLeagueIdAsync(
        Guid teamId, CancellationToken cancellation)
    {
        return _dbContext.Set<FantasyTeam>()
            .Where(team => team.Id == teamId)
            .Select(team => team.LeagueId)
            .SingleAsync(cancellation);
    }

    private async Task EnsurePlayerIsAvailableAsync(
        Guid leagueId, Guid playerId, CancellationToken cancellation)
    {
        var isAssigned = await _dbContext.Set<FantasyTeamPlayer>()
            .AsNoTracking()
            .AnyAsync(player =>
                player.LeagueId == leagueId && player.NbaPlayerId == playerId,
                cancellation);

        if (isAssigned)
            throw new ConflictException("The player is already assigned to a team in this league.");
    }
}
