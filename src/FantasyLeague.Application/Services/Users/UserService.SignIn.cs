using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Mappings;

namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService
{
    public async Task<UserResponse> SignInAsync(
        SignInRequest req,
        CancellationToken cancellation = default)
    {
        req = req.NormalizeSignInRequest();
        req.ValidateSignInRequest();

        var identifierType = req.Identifier.DetermineIdentifierType();

        var identifierTypeString = identifierType switch
        {
            SignInIdentifierType.Username => "username",
            SignInIdentifierType.Email => "email",
            _ => throw new BadRequestException(
                "The identifier must be a valid username or email.")
        };

        var user = await GetUserAsync(
            identifierType,
            req.Identifier,
            identifierTypeString,
            cancellation);

        if (!_passwordHasher.Verify(req.Password, user.Password))
        {
            throw new UnauthorizedException("Password is incorrect.");
        }

        return user.ToResponse();
    }

    private async Task<User> GetUserAsync(
        SignInIdentifierType type,
        string identifierValue,
        string identifier,
        CancellationToken cancellation)
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
}
