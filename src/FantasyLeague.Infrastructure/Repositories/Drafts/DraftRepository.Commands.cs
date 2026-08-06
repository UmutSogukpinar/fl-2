using FantasyLeague.Domain.Entities.FantasyTeams;

using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.Drafts;

public sealed partial class DraftRepository
{
    public Task AddRosterPlayerAsync(
        FantasyTeamPlayer player,
        CancellationToken cancellation)
    {
        return dbContext.Set<FantasyTeamPlayer>()
            .AddAsync(player, cancellation)
            .AsTask();
    }

    public async Task<bool> TrySaveChangesAsync(
        CancellationToken cancellation)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellation);
            return true;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }
}
