using FantasyLeague.Domain.Entities.Leagues;

namespace FantasyLeague.Infrastructure.Repositories.Leagues;

using FantasyLeague.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed partial class LeagueRepository
{
    public Task AddAsync(League league, CancellationToken cancellation)
    {
        return dbContext.Set<League>()
            .AddAsync(league, cancellation)
            .AsTask();
    }

    public void Remove(League league)
    {
        dbContext.Set<League>().Remove(league);
    }

    public Task SaveChangesAsync(CancellationToken cancellation)
    {
        return dbContext.SaveChangesAsync(cancellation);
    }

    public async Task<bool> RecordDraftFailureAsync(
        Guid leagueId,
        int cancellationThreshold,
        DateTime utcNow,
        CancellationToken cancellation)
    {
        var league = await dbContext.Set<League>()
            .SingleOrDefaultAsync(item => item.Id == leagueId, cancellation);

        if (league is null || league.Status != LeagueStatus.Drafting)
        {
            return league?.Status == LeagueStatus.DraftCancelled;
        }

        league.ConsecutiveDraftFailureCount++;
        league.UpdatedAt = utcNow;

        if (league.ConsecutiveDraftFailureCount >= cancellationThreshold)
        {
            league.Status = LeagueStatus.DraftCancelled;
        }

        return league.Status == LeagueStatus.DraftCancelled;
    }
}
