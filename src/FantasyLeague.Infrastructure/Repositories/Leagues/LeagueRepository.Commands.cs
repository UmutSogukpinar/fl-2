namespace FantasyLeague.Infrastructure.Repositories;

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
}
