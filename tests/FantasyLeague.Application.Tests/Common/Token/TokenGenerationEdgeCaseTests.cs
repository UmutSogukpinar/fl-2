using FantasyLeague.Application.Common.Token;

namespace FantasyLeague.Application.Tests.Common.Token;

public sealed class TokenGenerationEdgeCaseTests
{
    [Fact]
    public void GenerateRefreshToken_ProducesCryptographicallySizedPayload()
    {
        var token = TokenGeneration.GenerateRefreshToken();

        Assert.Equal(64, Convert.FromBase64String(token).Length);
    }

    [Fact]
    public void GenerateRefreshToken_CalledRepeatedly_ProducesUniqueValues()
    {
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => TokenGeneration.GenerateRefreshToken())
            .ToArray();

        Assert.Equal(tokens.Length, tokens.Distinct().Count());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("refresh-token")]
    [InlineData("çığ-şifre-🔐")]
    public void HashToken_ForAnyText_IsDeterministicSha256Hex(string token)
    {
        var first = token.HashToken();
        var second = token.HashToken();

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.Matches("^[0-9A-F]{64}$", first);
    }

    [Theory]
    [InlineData("")]
    [InlineData("regular-token")]
    [InlineData("token-with-unicode-ğüşi")]
    public void VerifyToken_WithMatchingHash_ReturnsTrue(string token)
    {
        Assert.True(TokenGeneration.VerifyToken(token, token.HashToken()));
    }

    [Fact]
    public void VerifyToken_WithDifferentToken_ReturnsFalse()
    {
        Assert.False(TokenGeneration.VerifyToken(
            "presented-token", "stored-token".HashToken()));
    }

    [Fact]
    public void HashToken_WithOneCharacterDifference_ChangesHash()
    {
        Assert.NotEqual("token-a".HashToken(), "token-b".HashToken());
    }
}
