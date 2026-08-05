using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.Repositories.Projections;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed partial class LeagueRepository
{
    public async Task<(IReadOnlyCollection<LeagueResponse> Items, int TotalCount)>
        GetPagedAsync(
            PaginationRequest request,
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
            .ApplyPagination(request)
            .Select(LeagueProjections.Response)
            .ToArrayAsync(cancellation);

        return (items, totalCount);
    }

    public Task<LeagueResponse?> GetResponseByIdAsync(
        Guid id, CancellationToken cancellation)
    {
        return dbContext.Set<League>().AsNoTracking()
            .Where(league => league.Id == id)
            .Select(LeagueProjections.Response)
            .SingleOrDefaultAsync(cancellation);
    }

    public Task<LeagueResponse?> GetResponseByJoinCodeAsync(
        string joinCode, CancellationToken cancellation)
    {
        return dbContext.Set<League>().AsNoTracking()
            .Where(league => league.JoinCode == joinCode)
            .Select(LeagueProjections.Response)
            .SingleOrDefaultAsync(cancellation);
    }

    public Task<League?> GetTrackedByIdAsync(
        Guid id, CancellationToken cancellation)
    {
        return dbContext.Set<League>()
            .Include(league => league.Settings)
            .SingleOrDefaultAsync(league => league.Id == id, cancellation);
    }

    public async Task<IReadOnlyList<League>> GetDueForDraftAsync(
        DateTime utcNow, CancellationToken cancellationToken)
    {
        return await dbContext.Set<League>()
            .Include(league => league.Settings)
            .Where(league => league.Settings.DraftDate <= utcNow)
            .Where(league => league.Status != LeagueStatus.Drafting)
            .Where(league => league.Status != LeagueStatus.DraftCancelled)
            .Where(league => league.Status != LeagueStatus.Active)
            .Where(league => league.Status != LeagueStatus.Completed)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<League>> GetDraftingAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<League>()
            .Include(league => league.Settings)
            .Where(league => league.Status == LeagueStatus.Drafting)
            .ToListAsync(cancellationToken);
    }
}
