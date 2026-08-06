using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Token;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Entities.Auth;
using FantasyLeague.Domain.Entities.Users;

namespace FantasyLeague.Application.Services.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IAuthService
{
    public async Task<(UserResponse user, string accessToken, string refreshToken)>
        SignInAsync(
            SignInRequest request,
            CancellationToken cancellationToken = default)
    {
        request = request.NormalizeSignInRequest();
        request.ValidateSignInRequest();

        var (identifierValue, identifierTypeName, identifierType) =
            ExtractIdentifier(request);
        var user = await GetUserAsync(
            identifierType,
            identifierValue,
            identifierTypeName,
            cancellationToken);

        if (!passwordHasher.Verify(request.Password, user.Password))
        {
            throw new UnauthorizedException("Password is incorrect.");
        }

        var jwtId = Guid.NewGuid().ToString();
        var accessToken = jwtService.GenerateToken(
            user.Username,
            GetUserRoles([user.Role]),
            jwtId);
        var refreshToken = TokenGeneration.GenerateRefreshToken();

        userRepository.AddRefreshToken(CreateRefreshToken(
            refreshToken,
            jwtId,
            user.Id));
        await userRepository.SaveChangesAsync(cancellationToken);

        return (user.ToResponse(), accessToken, refreshToken);
    }

    public async Task<(UserResponse user, string accessToken, string refreshToken)>
        RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Refresh token is missing.");
        }

        var storedToken = await userRepository.GetRefreshTokenAsync(
            refreshToken.HashToken(), cancellationToken);

        if (storedToken is null
            || storedToken.Status != TokenStatus.Active
            || storedToken.ExpiryDate <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        var user = await userRepository.GetTrackedByIdAsync(
            storedToken.UserId,
            cancellationToken)
            ?? throw new UnauthorizedException(
                "Refresh token user no longer exists.");

        Revoke(storedToken);

        var newJwtId = Guid.NewGuid().ToString();
        var accessToken = jwtService.GenerateToken(
            user.Username,
            GetUserRoles([user.Role]),
            newJwtId);
        var newRefreshToken = TokenGeneration.GenerateRefreshToken();

        userRepository.AddRefreshToken(CreateRefreshToken(
            newRefreshToken,
            newJwtId,
            user.Id));
        await userRepository.SaveChangesAsync(cancellationToken);

        return (user.ToResponse(), accessToken, newRefreshToken);
    }

    public async Task SignOutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var storedToken = await userRepository.GetRefreshTokenAsync(
            refreshToken.HashToken(), cancellationToken);

        if (storedToken is null || storedToken.Status != TokenStatus.Active) return;

        Revoke(storedToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    private static RefreshToken CreateRefreshToken(
        string rawToken,
        string jwtId,
        Guid userId) => new()
    {
        Token = rawToken.HashToken(),
        JwtId = jwtId,
        ExpiryDate = DateTime.UtcNow.AddDays(7),
        Status = TokenStatus.Active,
        UserId = userId
    };

    private static void Revoke(RefreshToken refreshToken)
    {
        refreshToken.Status = TokenStatus.Inactive;
        refreshToken.RevokeDate = DateTime.UtcNow;
    }

    private static (
        string identifierValue,
        string identifierTypeName,
        SignInIdentifierType identifierType) ExtractIdentifier(SignInRequest request)
    {
        var identifierType = request.Identifier.DetermineIdentifierType();
        var identifierTypeName = identifierType switch
        {
            SignInIdentifierType.Username => "username",
            SignInIdentifierType.Email => "email",
            _ => throw new BadRequestException(
                "The identifier must be a valid username or email.")
        };

        return (request.Identifier, identifierTypeName, identifierType);
    }

    private async Task<User> GetUserAsync(
        SignInIdentifierType identifierType,
        string identifierValue,
        string identifierTypeName,
        CancellationToken cancellationToken)
    {
        return identifierType switch
        {
            SignInIdentifierType.Username =>
                await userRepository.GetByUsernameAsync(
                    identifierValue,
                    cancellationToken),
            SignInIdentifierType.Email =>
                await userRepository.GetByEmailAsync(
                    identifierValue,
                    cancellationToken),
            _ => throw new BadRequestException(
                "The identifier must be a valid username or email.")
        } ?? throw new UnauthorizedException(
            $"The {identifierTypeName} is incorrect.");
    }

    private static IEnumerable<string> GetUserRoles(UserRole[] roles) =>
        roles.Select(role => role.ToString());
}
