using FantasyLeague.Domain.Entities.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FantasyLeague.Application.Services.Auth;

public sealed class JwtService(
    IOptions<JwtTokenOptions> options) : IJwtService
{
    private readonly JwtTokenOptions _options = options.Value;

    public string GenerateToken(
        string userName,
        IEnumerable<string> roles)
    {
        var secretBytes = Encoding.UTF8.GetBytes(
            _options.Secret);

        var securityKey = new SymmetricSecurityKey(
            secretBytes);

        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                userName),

            new(
                JwtRegisteredClaimNames.UniqueName,
                userName),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        claims.AddRange(
            roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(
                _options.ExpiryMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public bool VerifyToken(string token)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Secret));

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,

                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,

                ValidateAudience = true,
                ValidAudience = _options.Audience,

                ValidateLifetime = true,

                ValidAlgorithms =
                [
                    SecurityAlgorithms.HmacSha256
                ],

                ClockSkew = TimeSpan.FromSeconds(30)
            };

        try
        {
            var tokenHandler =
                new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(
                token,
                validationParameters,
                out _);

            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
