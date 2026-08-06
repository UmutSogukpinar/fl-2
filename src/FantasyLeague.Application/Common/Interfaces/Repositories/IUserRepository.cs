using FantasyLeague.Domain.Entities.Users;
using FantasyLeague.Domain.Entities.Auth;

using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<(IReadOnlyCollection<UserResponse> Items, int TotalCount)> GetPagedAsync(PaginationRequest request, CancellationToken cancellation);
    Task<UserResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellation);
    Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellation);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellation);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellation);
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellation);
    Task<bool> ExistsAsync(string username, string email, Guid? excludedUserId, CancellationToken cancellation);
    User Add(User user);
    void AddRefreshToken(RefreshToken refreshToken);
    void Remove(User user);
    Task SaveChangesAsync(CancellationToken cancellation);
}
