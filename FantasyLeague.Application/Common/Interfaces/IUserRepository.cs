using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    void Remove(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
