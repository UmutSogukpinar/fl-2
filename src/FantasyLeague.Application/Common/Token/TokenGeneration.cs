using System.Security.Cryptography;
using System.Text;

namespace FantasyLeague.Application.Common.Token;

public static class TokenGeneration
{
    public static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }

    public static string HashToken(this string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    public static bool VerifyToken(string token, string hashedToken)
    {
        string hashedInputToken = token.HashToken();

        var hashedInputTokenBytes = Convert.FromHexString(hashedInputToken);
        var hashedTokenBytes = Convert.FromHexString(hashedToken);

        return CryptographicOperations.FixedTimeEquals(
            hashedInputTokenBytes,
            hashedTokenBytes
        );
    }

}
