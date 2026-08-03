using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.Repositories.Projections;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class LeagueRepository(AppDbContext dbContext) : ILeagueRepository
{
    public async Task<(IReadOnlyCollection<LeagueResponse> Items, int TotalCount)>
    GetPagedAsync(
        int pageNumber,
        int pageSize,
        LeagueStatus? status,
        CancellationToken cancellation)
    {
        var query = dbContext.Set<League>().AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(league => league.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellation);
        var items = await query
            .OrderByDescending(league => league.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(LeagueProjections.Response)
            .ToArrayAsync(cancellation);

        return (items, totalCount);
    }

    public Task<LeagueResponse?> GetResponseByIdAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return dbContext.Set<League>()
            .AsNoTracking()
            .Where(league => league.Id == id)
            .Select(LeagueProjections.Response)
            .SingleOrDefaultAsync(cancellation);
    }

    public Task<LeagueResponse?> GetResponseByJoinCodeAsync(
        string joinCode,
        CancellationToken cancellation)
    {
        return dbContext.Set<League>()
            .AsNoTracking()
            .Where(league => league.JoinCode == joinCode)
            .Select(LeagueProjections.Response)
            .SingleOrDefaultAsync(cancellation);
    }

    public Task<League?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return dbContext.Set<League>()
            .Include(league => league.Settings)
            .SingleOrDefaultAsync(
                league => league.Id == id, cancellation
            );
    }

    public async Task<IReadOnlyList<League>> GetDueForDraftAsync(
        DateTime utcNow,
        CancellationToken cancellationToken
    )
    {
        return await dbContext.Set<League>()
            .Include(league => league.Settings)
            .Where(league =>
                league.Settings.DraftDate <= utcNow
                && league.Status != LeagueStatus.Drafting
                && league.Status != LeagueStatus.Active
                && league.Status != LeagueStatus.Completed)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<League>> GetDraftingAsync(
        CancellationToken cancellationToken
    )
    {
        return await dbContext.Set<League>()
            .Include(league => league.Settings)
            .Where(league => league.Status == LeagueStatus.Drafting)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(
        League league,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Set<League>()
            .AddAsync(league, cancellationToken)
            .AsTask();
    }

    public void Remove(League league)
    {
        dbContext.Set<League>().Remove(league);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken
    )
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
