namespace FantasyLeague.Domain.Entities.Auth;

public class JwtTokenOptions
{
    public string Secret { get; set; } = null!;
    public int ExpiryMinutes { get; set; }
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
}
