using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class LeagueRepository(AppDbContext dbContext) : ILeagueRepository
{
    public async Task<IReadOnlyCollection<LeagueResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<League>()
            .AsNoTracking()
            .OrderByDescending(league => league.CreatedAt)
            .Select(league => new LeagueResponse(
                league.Id,
                league.Name,
                league.Description,
                league.Season,
                league.MaxTeams,
                league.CommissionerId,
                league.Status,
                league.DraftDate,
                league.JoinCode,
                league.CreatedAt,
                league.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public Task<LeagueResponse?> GetResponseByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<League>()
            .AsNoTracking()
            .Where(league => league.Id == id)
            .Select(league => new LeagueResponse(
                league.Id,
                league.Name,
                league.Description,
                league.Season,
                league.MaxTeams,
                league.CommissionerId,
                league.Status,
                league.DraftDate,
                league.JoinCode,
                league.CreatedAt,
                league.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<League?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Set<League>()
            .SingleOrDefaultAsync(league => league.Id == id, cancellationToken);
    }

    public Task AddAsync(League league, CancellationToken cancellationToken)
    {
        return dbContext.Set<League>().AddAsync(league, cancellationToken).AsTask();
    }

    public void Remove(League league)
    {
        dbContext.Set<League>().Remove(league);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
