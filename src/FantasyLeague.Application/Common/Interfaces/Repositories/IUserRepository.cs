using FantasyLeague.Domain.Entities.Users;
using FantasyLeague.Domain.Entities.Auth;

using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<(IReadOnlyCollection<UserResponse> Items, int TotalCount)> GetPagedAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<UserResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string username, string email, Guid? excludedUserId, CancellationToken cancellationToken);
    User Add(User user);
    void AddRefreshToken(RefreshToken refreshToken);
    void Remove(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
