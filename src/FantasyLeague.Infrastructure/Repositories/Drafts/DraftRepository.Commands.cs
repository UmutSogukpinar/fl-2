using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed partial class DraftRepository
{
    public Task AddRosterPlayerAsync(
        FantasyTeamPlayer player,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<FantasyTeamPlayer>()
            .AddAsync(player, cancellationToken)
            .AsTask();
    }

    public async Task<bool> TrySaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
