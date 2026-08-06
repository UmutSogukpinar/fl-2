using FantasyLeague.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.Users;

public sealed partial class UserRepository
{
    public User Add(User user)
    {
        dbContext.Set<User>().Add(user);
        return user;
    }

    public void Remove(User user)
    {
        dbContext.Set<User>().Remove(user);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
