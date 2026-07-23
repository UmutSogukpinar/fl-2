using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<User>().AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(user => user.Username)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (users, totalCount);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Set<User>()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = username.ToLower();

        return dbContext.Set<User>().AnyAsync(
            user => (!excludedUserId.HasValue || user.Id != excludedUserId.Value)
                && (user.Username.ToLower() == normalizedUsername || user.Email == email),
            cancellationToken);
    }

    public User Add(User user)
    {
        dbContext.Set<User>().Add(user);

        return user;
    }

    public void Remove(User user)
    {
        dbContext.Set<User>().Remove(user);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
