using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class LeagueRepository(AppDbContext dbContext) : ILeagueRepository
{
    public async Task<IReadOnlyCollection<League>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<League>()
            .AsNoTracking()
            .OrderByDescending(league => league.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task<League?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
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
