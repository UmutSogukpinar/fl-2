using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.Users;

public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : IUserService
{
    public async Task<PagedResponse<UserResponse>> GetAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await userRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = users.Select(Map).ToArray();
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
        return Map(user);
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = NormalizeUsername(request.Username);
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);

        await EnsureUniqueAsync(username, email, null, cancellationToken);

        var user = new User
        {
            Username = username,
            Email = email,
            Password = passwordHasher.Hash(request.Password)
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Map(user);
    }

    public async Task<UserResponse> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);
        var username = NormalizeUsername(request.Username);
        var email = NormalizeEmail(request.Email);

        await EnsureUniqueAsync(username, email, id, cancellationToken);

        user.Username = username;
        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);
        userRepository.Remove(user);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"User '{id}' was not found.");
    }

    private async Task EnsureUniqueAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsAsync(
                username,
                email,
                excludedUserId,
                cancellationToken))
        {
            throw new ConflictException("The username or email is already in use.");
        }
    }

    private static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BadRequestException("Username is required.");
        }

        return username.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException("Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BadRequestException("Password is required.");
        }
    }

    private static UserResponse Map(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.CreatedAt,
        user.UpdatedAt);
}
