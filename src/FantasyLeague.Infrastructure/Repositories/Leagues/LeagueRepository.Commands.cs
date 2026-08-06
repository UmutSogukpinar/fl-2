using FantasyLeague.Domain.Entities.Leagues;

namespace FantasyLeague.Infrastructure.Repositories.Leagues;

using FantasyLeague.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed partial class LeagueRepository
{
    public Task AddAsync(League league, CancellationToken cancellationToken)
    {
        return dbContext.Set<League>()
            .AddAsync(league, cancellationToken)
            .AsTask();
    }

    public void Remove(League league)
    {
        dbContext.Set<League>().Remove(league);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RecordDraftFailureAsync(
        Guid leagueId,
        int cancellationThreshold,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var league = await dbContext.Set<League>()
            .SingleOrDefaultAsync(item => item.Id == leagueId, cancellationToken);

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

        await dbContext.SaveChangesAsync(cancellationToken);
        return league.Status == LeagueStatus.DraftCancelled;
    }
}
