using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Pagination;

namespace FantasyLeague.Application.Services.Users;

public sealed class UserService(
    IUserRepository _userRepository,
    IPasswordHasher _passwordHasher) : IUserService
{
    public async Task<PagedResponse<UserResponse>> GetAsync(
        PaginationRequest req,
        CancellationToken cancellationToken = default)
    {
        req.ValidatePaginationRequest();

        var (items, totalCount) = await _userRepository.GetPagedAsync(
            req.PageNumber,
            req.PageSize,
            cancellationToken);

        return new PagedResponse<UserResponse>(
            items,
            req.PageNumber,
            req.PageSize,
            totalCount,
            totalCount.CalculateTotalPage(req.PageSize));
    }

    public async Task<UserResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetResponseByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"User '{id}' was not found.");
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest req,
        CancellationToken cancellationToken = default)
    {
        req = req.NormalizeCreateUserRequest();
        req.ValidateCreateUserRequest();

        await EnsureUniqueAsync(
            req.Username,
            req.Email,
            null,
            cancellationToken
        );

        var user = req.ToEntity(_passwordHasher.Hash(req.Password));

        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user.ToResponse();
    }

    public async Task<UserResponse> SignInAsync(
        SignInRequest req,
        CancellationToken cancellation = default)
    {
        req = req.NormalizeSignInRequest();
        req.ValidateSignInRequest();
        var user = await _userRepository.GetByEmailAsync(req.Email, cancellation);

        if (user is null || !_passwordHasher.Verify(req.Password, user.Password))
            throw new UnauthorizedException(
                "The email or password is incorrect."
            );

        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateAsync(
        Guid id,
        UpdateUserRequest req,
        CancellationToken cancellation = default)
    {
        var user = await GetTrackedUserOrThrowAsync(id, cancellation);

        req = req.NormalizeUpdateUserRequest();
        req.ValidateUpdateUserRequest();

        await EnsureUniqueAsync(req.Username, req.Email, id, cancellation);

        req.MapTo(user);

        await _userRepository.SaveChangesAsync(cancellation);
        return user.ToResponse();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        var user = await GetTrackedUserOrThrowAsync(id, cancellation);
        _userRepository.Remove(user);
        await _userRepository.SaveChangesAsync(cancellation);
    }

    private async Task<User> GetTrackedUserOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await _userRepository.GetTrackedByIdAsync(id, cancellation)
            ?? throw new NotFoundException($"User '{id}' was not found.");
    }

    private async Task EnsureUniqueAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsAsync(
                username,
                email,
                excludedUserId,
                cancellationToken))
        {
            throw new ConflictException(
                "The username or email is already in use."
            );
        }
    }

}
