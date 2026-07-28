using FantasyLeague.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FantasyLeague.Application.Common.Normalization;

internal static class UserNormalization
{
    public static CreateUserRequest NormalizeCreateUserRequest(
         this CreateUserRequest req
    )
    {
        return req with
        {
            Username = NormalizeUsername(req.Username),
            Email = NormalizeEmail(req.Email)
        };
    }

    public static UpdateUserRequest NormalizeUpdateUserRequest(
        this UpdateUserRequest req
    )
    {
        return req with
        {
            Username = NormalizeUsername(req.Username),
            Email = NormalizeEmail(req.Email)
        };
    }

    public static SignInRequest NormalizeSignInRequest(
        this SignInRequest req
    )
    {
        return req with
        {
            Email = NormalizeEmail(req.Email)
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
