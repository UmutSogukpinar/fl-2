using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.Repositories.Projections;
using FantasyLeague.Application.Models;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class FantasyTeamRepository(AppDbContext dbContext) : IFantasyTeamRepository
{
    public async Task<(IReadOnlyCollection<FantasyTeamResponse> Items, int TotalCount)> GetPagedByLeagueIdAsync(
        Guid leagueId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.LeagueId == leagueId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(team => team.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(FantasyTeamProjections.Response)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<FantasyTeamResponse?> GetResponseByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.Id == id)
            .Select(FantasyTeamProjections.Response)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<FantasyTeam?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Set<FantasyTeam>()
            .SingleOrDefaultAsync(team => team.Id == id, cancellationToken);
    }

    public Task<int> CountByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Set<FantasyTeam>()
            .CountAsync(team => team.LeagueId == leagueId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetIdsByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.LeagueId == leagueId)
            .Select(team => team.Id)
            .ToListAsync(cancellationToken);
    }

    // TODO: update
    public Task<FastasyTeamConflictResult> ExistsAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellation)
    {
        var conflict = dbContext.Set<FantasyTeam>()
            .Where(team => team.LeagueId == leagueId)
            .GroupBy(team => team.OwnerId)
    }

    public Task AddAsync(FantasyTeam team, CancellationToken cancellationToken)
    {
        return dbContext.Set<FantasyTeam>()
            .AddAsync(team, cancellationToken).AsTask();
    }

    public void Remove(FantasyTeam team)
    {
        dbContext.Set<FantasyTeam>().Remove(team);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
