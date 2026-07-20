using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class FantasyTeamRepository(AppDbContext dbContext) : IFantasyTeamRepository
{
    public async Task<IReadOnlyCollection<FantasyTeam>> GetByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.LeagueId == leagueId)
            .OrderBy(team => team.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<FantasyTeam?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Set<FantasyTeam>()
            .SingleOrDefaultAsync(team => team.Id == id, cancellationToken);
    }

    public Task<int> CountByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken)
    {
        return dbContext.Set<FantasyTeam>()
            .CountAsync(team => team.LeagueId == leagueId, cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToLower();

        return dbContext.Set<FantasyTeam>().AnyAsync(
            team => team.LeagueId == leagueId
                && (!excludedTeamId.HasValue || team.Id != excludedTeamId.Value)
                && (team.OwnerId == ownerId || team.Name.ToLower() == normalizedName),
            cancellationToken);
    }

    public Task AddAsync(FantasyTeam team, CancellationToken cancellationToken)
    {
        return dbContext.Set<FantasyTeam>().AddAsync(team, cancellationToken).AsTask();
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
