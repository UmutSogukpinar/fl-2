using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<(IReadOnlyCollection<UserResponse> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<UserResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string username, string email, Guid? excludedUserId, CancellationToken cancellationToken);
    User Add(User user);
    void Remove(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
