using FantasyLeague.Domain.Entities.Users;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;

namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService
{
    public async Task<PagedResponse<UserResponse>> GetAsync(
        PaginationRequest req,
        CancellationToken cancellation = default)
    {
        req.ValidatePaginationRequest();

        var (items, totalCount) = await _userRepository.GetPagedAsync(
            req,
            cancellation);

        return Pagination.CreateResponse(items, totalCount, req);
    }

    public async Task<UserResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        return await _userRepository.GetResponseByIdAsync(
            id, cancellation)
            ?? throw new NotFoundException(
                $"User '{id}' was not found.");
    }

}
