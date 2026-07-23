using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Common.Validation;

internal class UserValidation
{
    public static void ValidateCreateUserRequest(CreateUserRequest request){
        ValidateEmail(request.Email);
        ValidatePassword(request.Password);
        ValidateUsername(request.Username);
    }

    public static void ValidateUpdateUserRequest(UpdateUserRequest request)
    {
        ValidateEmail(request.Email);
        ValidateUsername(request.Username);
    }

    private static void ValidateEmail(string email){
        const char AtSymbol = '@';
        const char DotSymbol = '.';

        if (string.IsNullOrWhiteSpace(email)){
            throw new BadRequestException("Email cannot be empty.");
        }

        var atIndex = email.IndexOf(AtSymbol);
        if (atIndex <= 0 || atIndex == email.Length - 1){
            throw new BadRequestException("Email must contain a valid '@' symbol.");
        }

        var domainPart = email.Substring(atIndex + 1);
        if (!domainPart.Contains(DotSymbol)){
            throw new BadRequestException(
                "Email domain must contain a '.' symbol."
            );
        }
    }
    private static void ValidatePassword(string password)
    {
        const int MaxLength = 128;
        const int MinLength = 8;

        if (string.IsNullOrWhiteSpace(password)) {
            throw new BadRequestException(
                "Password cannot be empty."
            );
        }

        if (password.Length > MaxLength) {
            throw new BadRequestException(
                $"Password cannot exceed {MaxLength} characters."
            );
        }

        if (password.Length < MinLength) {
            throw new BadRequestException(
                $"Password must be at least {MinLength} characters long."
            );
        }
    }

    private static void ValidateUsername(string username)
    {
        const int MinUsernameLength = 4;

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BadRequestException("Username cannot be empty.");
        }
        if (username.Length < MinUsernameLength)
        {
            throw new BadRequestException(
                $"Username must be at least " +
                $"{MinUsernameLength} characters long."
            );
        }
    }
}

