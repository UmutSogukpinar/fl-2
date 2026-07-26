namespace FantasyLeague.Application.Common.Interfaces.Security;

public interface IPasswordHasher
{
    string Hash(string password);
}
