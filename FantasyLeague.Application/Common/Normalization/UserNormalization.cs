using FantasyLeague.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Common.Normalization;

internal static class UserNormalization
{
    public static void NormalizeCreateUserRequest(
        ref CreateUserRequest request
    )
    {
        request = request with
        {
            Username = NormalizeUsername(request.Username),
            Email = NormalizeEmail(request.Email)
        };
    }

    public static void NormalizeUpdateUserRequest(
        ref UpdateUserRequest request
    )
    {
        request = request with
        {
            Username = NormalizeUsername(request.Username),
            Email = NormalizeEmail(request.Email)
        };
    }

    public static void NormalizeSignInRequest(ref SignInRequest request)
    {
        request = request with
        {
            Email = NormalizeEmail(request.Email)
        };
    }

    // ================== Utils ==================

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant()!;
    }

    private static string NormalizeUsername(string? username)
    {
        return username?.Trim()!;
    }
}
