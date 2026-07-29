using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.Repositories.Projections;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<(IReadOnlyCollection<UserResponse> Items, int TotalCount)> GetPagedAsync(
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
            .Select(UserProjections.Response)
            .ToArrayAsync(cancellationToken);

        return (users, totalCount);
    }

    public Task<UserResponse?> GetResponseByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<User>()
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(UserProjections.Response)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Set<User>()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Set<User>()
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
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
