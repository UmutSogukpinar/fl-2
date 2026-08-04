using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.Users;

namespace FantasyLeague.Application.Common.Validation;

internal static class UserValidation
{
    public static void ValidateCreateUserRequest(
        this CreateUserRequest request
    )
    {
        if (request == null)
        {
            throw new BadRequestException(
                "CreateUserRequest cannot be null."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new BadRequestException(
                "Username must be provided."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException(
                "Password must be provided."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new BadRequestException(
                "Email must be provided."
            );
        }

        ValidateEmail(request.Email);
        ValidatePassword(request.Password);
        ValidateUsername(request.Username);
    }

    public static void ValidateUpdateUserRequest(
        this UpdateUserRequest request
    )
    {
        if (request == null)
        {
            throw new BadRequestException(
                "UpdateUserRequest cannot be null."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new BadRequestException(
                "Email must be provided."
            );
        }

        if (string.IsNullOrEmpty(request.Username))
        {
            throw new BadRequestException(
                "Username must be provided."
            );
        }

        ValidateEmail(request.Email);
        ValidateUsername(request.Username);
    }

    public static void ValidateSignInRequest(this SignInRequest request)
    {
        var identifierType = DetermineIdentifierType(request.Identifier);

        if (identifierType.HasFlag(SignInIdentifierType.Username))
        {
            ValidateUsername(request.Identifier);
        }

        if (identifierType.HasFlag(SignInIdentifierType.Email))
        {
            ValidateEmail(request.Identifier);
        }

        ValidatePassword(request.Password);
    }

    // ========================= Utils =========================

    public static SignInIdentifierType DetermineIdentifierType(
        this string Identifier
    )
    {
        if (string.IsNullOrWhiteSpace(Identifier))
        {
            throw new BadRequestException(
                "Identifier must be provided."
            );
        }

        if (Identifier.Contains('@'))
        {
            return SignInIdentifierType.Email;
        }

        return SignInIdentifierType.Username;
    }

    private static void ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        const char AtSymbol = '@';
        const char DotSymbol = '.';


        var atIndex = email.IndexOf(AtSymbol);
        if (atIndex <= 0 || atIndex == email.Length - 1)
        {
            throw new BadRequestException(
                "Email must contain a valid '@' symbol."
            );
        }

        var domainPart = email[(atIndex + 1)..];

        if (!domainPart.Contains(DotSymbol))
        {
            throw new BadRequestException(
                "Email domain must contain a '.' symbol."
            );
        }
    }

    private static void ValidatePassword(string password)
    {
        const int MaxLength = 128;
        const int MinLength = 8;

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BadRequestException(
                "Password cannot be empty."
            );
        }

        if (password.Length > MaxLength)
        {
            throw new BadRequestException(
                $"Password cannot exceed {MaxLength} characters."
            );
        }

        if (password.Length < MinLength)
        {
            throw new BadRequestException(
                $"Password must be at least {MinLength} characters long."
            );
        }
    }

    private static void ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;

        const int MinUsernameLength = 4;

        if (username.Length < MinUsernameLength)
        {
            throw new BadRequestException(
                $"Username must be at least " +
                $"{MinUsernameLength} characters long."
            );
        }
    }
}

