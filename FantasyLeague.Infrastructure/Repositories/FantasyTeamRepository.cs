using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class FantasyTeamRepository(AppDbContext dbContext) : IFantasyTeamRepository
{
    public async Task<IReadOnlyCollection<FantasyTeamResponse>> GetByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.LeagueId == leagueId)
            .OrderBy(team => team.Name)
            .Select(team => new FantasyTeamResponse(
                team.Id,
                team.Name,
                team.LeagueId,
                team.OwnerId,
                team.CreatedAt,
                team.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public Task<FantasyTeamResponse?> GetResponseByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Set<FantasyTeam>()
            .AsNoTracking()
            .Where(team => team.Id == id)
            .Select(team => new FantasyTeamResponse(
                team.Id,
                team.Name,
                team.LeagueId,
                team.OwnerId,
                team.CreatedAt,
                team.UpdatedAt))
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

    public Task<bool> ExistsAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellation)
    {
        var normalizedName = name.ToLower();
        return dbContext.Set<FantasyTeam>().AnyAsync(
            team => team.LeagueId == leagueId
                && (!excludedTeamId.HasValue || team.Id != excludedTeamId.Value)
                && (team.OwnerId == ownerId || team.Name.ToLower() == normalizedName),
            cancellation);
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
