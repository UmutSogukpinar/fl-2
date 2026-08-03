using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Players;
using FantasyLeague.Domain.Entities.Users;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.IntegrationTests.Database;

public static class IntegrationTestData
{
    public static async Task<(User Commissioner, User Owner, League League)> AddLeagueAsync(
        AppDbContext context,
        LeagueStatus status = LeagueStatus.RegistrationOpen,
        DateTime? draftDate = null)
    {
        var commissioner = CreateUser("commissioner");
        var owner = CreateUser("owner");
        var league = new League
        {
            Name = "Integration League",
            Season = 2026,
            CommissionerId = commissioner.Id,
            JoinCode = "INT12345",
            Status = status,
            Settings = new LeagueSettings
            {
                RosterSize = 12,
                DraftDate = draftDate
            }
        };

        context.AddRange(commissioner, owner, league);
        await context.SaveChangesAsync();
        return (commissioner, owner, league);
    }

    public static User CreateUser(string key) => new()
    {
        Username = key,
        Email = $"{key}@example.com",
        Password = "hashed-password"
    };

    public static NbaPlayer CreatePlayer(int nbaId, string firstName = "Test") => new()
    {
        NbaId = nbaId,
        FirstName = firstName,
        LastName = "Player",
        Team = "TST",
        Position = "G"
    };
}
