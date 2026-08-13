namespace FantasyLeague.Application.DTOs.Requests.Users;

public sealed record SignInRequest(
    string Identifier,
    string Password
);

[Flags]
public enum SignInIdentifierType
{
    None = 0,
    Username = 1 << 0,
    Email = 1 << 1
}
