using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.Mappings;

namespace FantasyLeague.Application.Services.Users;

public sealed class UserService(
    IUserRepository _userRepository,
    IPasswordHasher _passwordHasher) : IUserService
{
    public async Task<PagedResponse<UserResponse>> GetAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await _userRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = users.Select(user => user.ToResponse()).ToArray();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<UserResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<UserResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);
        return user.ToResponse();
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        UserValidation.ValidateCreateUserRequest(request);

        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        await EnsureUniqueAsync(
            username,
            email,
            null,
            cancellationToken
        );

        var user = request.ToEntity(_passwordHasher.Hash(request.Password));

        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);

        UserValidation.ValidateUpdateUserRequest(request);

        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        await EnsureUniqueAsync(username, email, id, cancellationToken);

        request.MapTo(user);

        await _userRepository.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);
        _userRepository.Remove(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _userRepository.GetByIdAsync(id, cancellationToken)
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
            throw new ConflictException("The username or email is already in use.");
        }
    }

}
