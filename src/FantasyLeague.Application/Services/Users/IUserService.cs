using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;

namespace FantasyLeague.Application.Services.Users;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetAsync(PaginationRequest request, CancellationToken cancellation = default);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellation = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellation = default);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellation = default);
    Task DeleteAsync(Guid id, CancellationToken cancellation = default);
}
