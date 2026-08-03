using FantasyLeague.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FantasyLeague.Application.Common.Normalization;

internal static class UserNormalization
{
    public static CreateUserRequest NormalizeCreateUserRequest(
         this CreateUserRequest? req
    )
    {
        req = EnsureRequestIsNotNull(req);

        return req with
        {
            Username = NormalizeUsername(req.Username),
            Email = NormalizeEmail(req.Email)
        };
    }

    public static UpdateUserRequest NormalizeUpdateUserRequest(
        this UpdateUserRequest? req
    )
    {
        req = EnsureRequestIsNotNull(req);

        return req with
        {
            Username = NormalizeUsername(req.Username),
            Email = NormalizeEmail(req.Email)
        };
    }

    public static SignInRequest NormalizeSignInRequest(
        this SignInRequest? req
    )
    {
        req = EnsureRequestIsNotNull(req);

        return req with
        {
            Email = NormalizeEmail(req.Email)
        };
    }

    // ================== Utils ==================

    private static T EnsureRequestIsNotNull<T>(T? request)
        where T : class
    {
        if (request is null)
        {
            throw new FantasyLeague.Application.Common.Exceptions.BadRequestException(
                "Request body is required.");
        }

        return request;
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant()!;
    }

    private static string NormalizeUsername(string? username)
    {
        return username?.Trim()!;
    }
}
