using FantasyLeague.Application.Services.Auth;
using FantasyLeague.Domain.Entities.Auth;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FantasyLeague.Application.Tests.Services.Auth;

public sealed class JwtServiceTests
{
    private const string Secret =
        "test-secret-key-that-is-at-least-thirty-two-characters-long";

    [Fact]
    public void GenerateToken_IncludesConfiguredClaimsAndMetadata()
    {
        var service = CreateService();
        var beforeGeneration = DateTime.UtcNow;

        var token = service.GenerateToken(
            "test-user",
            ["User", "Commissioner"],
            "jwt-id-123");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
        Assert.Equal("test-user", jwt.Subject);
        Assert.Equal(
            "jwt-id-123",
            jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value);
        Assert.Contains(jwt.Claims, claim =>
            claim.Type == ClaimTypes.Role && claim.Value == "User");
        Assert.Contains(jwt.Claims, claim =>
            claim.Type == ClaimTypes.Role && claim.Value == "Commissioner");
        Assert.InRange(
            jwt.ValidTo,
            beforeGeneration.AddMinutes(14),
            beforeGeneration.AddMinutes(16));
    }

    [Fact]
    public void VerifyToken_WithGeneratedToken_ReturnsTrue()
    {
        var service = CreateService();
        var token = service.GenerateToken("test-user", ["User"], "jwt-id");

        Assert.True(service.VerifyToken(token));
    }

    [Fact]
    public void VerifyToken_WithTamperedToken_ReturnsFalse()
    {
        var service = CreateService();
        var token = service.GenerateToken("test-user", ["User"], "jwt-id");
        var tamperedToken = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        Assert.False(service.VerifyToken(tamperedToken));
    }

    [Fact]
    public void VerifyToken_WithDifferentIssuer_ReturnsFalse()
    {
        var issuer = CreateService("first-issuer");
        var verifier = CreateService("second-issuer");
        var token = issuer.GenerateToken("test-user", ["User"], "jwt-id");

        Assert.False(verifier.VerifyToken(token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("one.two")]
    public void VerifyToken_WithMalformedToken_ReturnsFalse(string token)
    {
        Assert.False(CreateService().VerifyToken(token));
    }

    private static JwtService CreateService(string issuer = "test-issuer")
    {
        return new JwtService(Options.Create(new JwtTokenOptions
        {
            Secret = Secret,
            Issuer = issuer,
            Audience = "test-audience",
            ExpiryMinutes = 15
        }));
    }
}
