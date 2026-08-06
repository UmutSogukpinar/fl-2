using FantasyLeague.Domain.Entities.Users;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Application.Common.Token;


namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService
{
    public async
        Task<(UserResponse user,
            string accessToken,
            string refreshToken
            )>
        SignInAsync(
        SignInRequest req,
        CancellationToken cancellation = default)
    {
        req = req.NormalizeSignInRequest();
        req.ValidateSignInRequest();

        var (identifierValue,
            identifierTypeString,
            identifierType) = ExtractIdentifier(req);

        var user = await GetUserAsync(
            identifierType,
            identifierValue,
            identifierTypeString,
            cancellation);

        if (!_passwordHasher.Verify(req.Password, user.Password))
        {
            throw new UnauthorizedException("Password is incorrect.");
        }

        var roles = GetUserRolesInString([user.Role]);
        var accessToken = _jwtService.GenerateToken(
            user.Username, roles
        );
        var refreshToken = TokenGeneration.GenerateRefreshToken();

        return (user.ToResponse(), accessToken, refreshToken);
    }

    private static
        (string identifierValue,
        string identifierTypeString,
        SignInIdentifierType identifierType)
        ExtractIdentifier(
        SignInRequest req
    )
    {
        var identifierType = req.Identifier.DetermineIdentifierType();

        var identifierTypeString = identifierType switch
        {
            SignInIdentifierType.Username => "username",

            SignInIdentifierType.Email => "email",

            _ => throw new BadRequestException(
                "The identifier must be a valid username or email.")
        };

        return (req.Identifier, identifierTypeString, identifierType);
    }

    private async Task<User> GetUserAsync(
        SignInIdentifierType type,
        string identifierValue,
        string identifier,
        CancellationToken cancellation
    )
    {
        return type switch
        {
            SignInIdentifierType.Username =>
                await _userRepository.GetByUsernameAsync(
                    identifierValue,
                    cancellation),

            SignInIdentifierType.Email =>
                await _userRepository.GetByEmailAsync(
                    identifierValue,
                    cancellation),

            _ => throw new BadRequestException(
                "The identifier must be a valid username or email.")

        } ?? throw new UnauthorizedException(
            $"The {identifier} is incorrect.");
    }

    private static IEnumerable<string>
        GetUserRolesInString(UserRole[] roles)
    {
        return roles.Select(role => role.ToString());
    }
}
