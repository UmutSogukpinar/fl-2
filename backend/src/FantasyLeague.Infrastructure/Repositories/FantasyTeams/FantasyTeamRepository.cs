using FantasyLeague.Domain.Entities.FantasyTeams;

using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Infrastructure.Repositories.FantasyTeams;

public sealed partial class FantasyTeamRepository(
    AppDbContext _dbContext) : IFantasyTeamRepository
{
    public async Task<(IReadOnlyCollection<FantasyTeamResponse> Items, int TotalCount)>
        GetPagedByLeagueIdAsync(
        Guid leagueId,
        PaginationRequest request,
        CancellationToken cancellation
    )
    {
        var query = _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.LeagueId == leagueId);

        var totalCount = await query.CountAsync(cancellation);
        var items = await query
            .OrderBy(team => team.Name)
            .ApplyPagination(request)
            .Select(FantasyTeamProjections.Response)
            .ToArrayAsync(cancellation);

        return (items, totalCount);
    }

    public Task<FantasyTeamResponse?> GetResponseByIdAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.Id == id)
            .Select(FantasyTeamProjections.Response)
            .SingleOrDefaultAsync(cancellation);
    }

    public Task<FantasyTeam?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return _dbContext.Set<FantasyTeam>()
            .SingleOrDefaultAsync(team => team.Id == id, cancellation);
    }

    public Task<int> CountByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellation
    )
    {
        return _dbContext.Set<FantasyTeam>()
            .CountAsync(team => team.LeagueId == leagueId, cancellation);
    }

    public async Task<IReadOnlyList<Guid>> GetIdsByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        return await _dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.LeagueId == leagueId)
            .Select(team => team.Id)
            .ToListAsync(cancellation);
    }

    public async Task<FastasyTeamConflictResult> ExistsAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellation)
    {
        var conflict = await _dbContext.Set<FantasyTeam>()
            .Where(team =>
                team.LeagueId == leagueId
                && (!excludedTeamId.HasValue || team.Id != excludedTeamId.Value))
            .GroupBy(_ => 1)
            .Select(teams => new
            {
                OwnerHasMultipleTeam = teams.Any(team => team.OwnerId == ownerId),
                NameIsTaken = teams.Any(team => team.Name == name)
            })
            .SingleOrDefaultAsync(cancellation);

        if (conflict is null)
        {
            return FastasyTeamConflictResult.None;
        }

        var result = FastasyTeamConflictResult.None;

        if (conflict.OwnerHasMultipleTeam)
        {
            result |= FastasyTeamConflictResult.OwnerHasMultipleTeam;
        }

        if (conflict.NameIsTaken)
        {
            result |= FastasyTeamConflictResult.NameIsTaken;
        }

        return result;
    }

    public Task AddAsync(FantasyTeam team, CancellationToken cancellation)
    {
        return _dbContext.Set<FantasyTeam>()
            .AddAsync(team, cancellation).AsTask();
    }

    public void Remove(FantasyTeam team)
    {
        _dbContext.Set<FantasyTeam>().Remove(team);
    }

    public async Task SaveChangesAsync(CancellationToken cancellation)
    {
        await _dbContext.SaveChangesAsync(cancellation);
    }
}
