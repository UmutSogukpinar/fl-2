using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Infrastructure.Repositories.Users;

public sealed class UserRepository
    (AppDbContext dbContext) : IUserRepository
{
    public async 
        Task<(IReadOnlyCollection<UserResponse> Items, int TotalCount)> 
        GetPagedAsync(
            PaginationRequest request,
            CancellationToken cancellation
        )
    {
        var query = dbContext.Set<User>().AsNoTracking();
        var totalCount = await query.CountAsync(cancellation);
        var users = await query
            .OrderBy(user => user.Username)
            .ApplyPagination(request)
            .Select(UserProjections.Response)
            .ToArrayAsync(cancellation);

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

    public Task<User?> GetTrackedByIdAsync(
        Guid id, 
        CancellationToken cancellation)
    {
        return dbContext.Set<User>()
            .SingleOrDefaultAsync(user => user.Id == id, cancellation);
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellation
    )
    {
        return dbContext.Set<User>()
            .AsNoTracking()
            .Where(user => user.Email == email)
            .SingleOrDefaultAsync(cancellation);
    }

    public Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellation
    )
    {
        return dbContext.Set<User>()
            .AsNoTracking()
            .Where(user => user.Username.ToLower() == username)
            .SingleOrDefaultAsync(cancellation);
    }


    public Task<bool> ExistsAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellation
    )
    {
        var normalizedUsername = username.ToLower();

        return dbContext.Set<User>().AnyAsync(
            user => (!excludedUserId.HasValue || user.Id != excludedUserId.Value)
                && (user.Username.ToLower() == normalizedUsername ||
                       user.Email == email),
            cancellation);
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
