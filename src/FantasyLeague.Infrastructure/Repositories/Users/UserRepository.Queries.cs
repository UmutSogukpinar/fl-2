using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.Users;

public sealed partial class UserRepository
{
    public async Task<(IReadOnlyCollection<UserResponse> Items, int TotalCount)>
        GetPagedAsync(
            PaginationRequest request,
            CancellationToken cancellation)
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
        CancellationToken cancellation)
    {
        return dbContext.Set<User>()
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == email, cancellation);
    }

    public Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellation)
    {
        return dbContext.Set<User>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Username.ToLower() == username,
                cancellation);
    }

    public Task<bool> ExistsAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellation)
    {
        var normalizedUsername = username.ToLower();

        return dbContext.Set<User>()
            .Where(user => !excludedUserId.HasValue
                || user.Id != excludedUserId.Value)
            .AnyAsync(user => user.Username.ToLower() == normalizedUsername
                || user.Email == email, cancellation);
    }
}
