using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class LeagueRepository(AppDbContext dbContext) : ILeagueRepository
{
    public async Task<(IReadOnlyCollection<LeagueResponse> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<League>().AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(league => league.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(league => new LeagueResponse(
                league.Id,
                league.Name,
                league.Description,
                league.Season,
                league.MaxTeams,
                league.CommissionerId,
                league.Status,
                league.Settings.DraftDate,
                league.JoinCode,
                league.CreatedAt,
                league.UpdatedAt,
                league.Settings.RosterSize,
                league.Settings.DraftTimeZoneId))
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
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
                league.Settings.DraftDate,
                league.JoinCode,
                league.CreatedAt,
                league.UpdatedAt,
                league.Settings.RosterSize,
                league.Settings.DraftTimeZoneId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<LeagueResponse?> GetResponseByJoinCodeAsync(
        string joinCode,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<League>()
            .AsNoTracking()
            .Where(league => league.JoinCode == joinCode)
            .Select(league => new LeagueResponse(
                league.Id,
                league.Name,
                league.Description,
                league.Season,
                league.MaxTeams,
                league.CommissionerId,
                league.Status,
                league.Settings.DraftDate,
                league.JoinCode,
                league.CreatedAt,
                league.UpdatedAt,
                league.Settings.RosterSize,
                league.Settings.DraftTimeZoneId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<League?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Set<League>()
            .Include(league => league.Settings)
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
