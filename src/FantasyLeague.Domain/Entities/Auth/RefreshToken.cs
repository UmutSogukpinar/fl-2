using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Domain.Entities.Auth;

public class RefreshToken
{
    public string Token { get; set; } = null!;

    public string JwtId { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }

    public TokenStatus Status { get; set; }

    public required Guid UserId { get; set; }

}
