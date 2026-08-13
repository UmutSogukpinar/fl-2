using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Services.Auth;

public interface IJwtService
{
    string GenerateToken(
        string userName,
        IEnumerable<string> roles,
        string jwtId
    );

    bool VerifyToken(string token);
}
