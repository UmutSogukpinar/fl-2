using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FantasyLeague.Application.Common.Token;

public static class TokenGeneration
{
    public static string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }

    public static string HashToken(string token)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

}
