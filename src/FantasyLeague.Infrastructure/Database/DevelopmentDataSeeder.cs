using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Players;
using FantasyLeague.Domain.Entities.Users;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Database;

public static class DevelopmentDataSeeder
{
    public const string SeedPassword = "Fantasy123!";
    public const string CommissionerEmail = "commissioner@fantasyleague.local";

    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        if (await context.Set<User>().AnyAsync(
                user => user.Email == CommissionerEmail, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var users = new[]
        {
            CreateUser(1, "commissioner", CommissionerEmail, passwordHasher),
            CreateUser(2, "ankara-owner", "ankara@fantasyleague.local", passwordHasher),
            CreateUser(3, "izmir-owner", "izmir@fantasyleague.local", passwordHasher),
            CreateUser(4, "bursa-owner", "bursa@fantasyleague.local", passwordHasher)
        };
        var leagueId = Id(100);
        var league = new League
        {
            Id = leagueId,
            Name = "Development Super League",
            Description = "Local development and integration test data",
            Season = 2026,
            MaxTeams = 4,
            CommissionerId = users[0].Id,
            JoinCode = "DEV2026",
            Status = LeagueStatus.Active,
            CreatedAt = now.AddDays(-30),
            Settings = new LeagueSettings
            {
                LeagueId = leagueId,
                RosterSize = 3,
                DraftDate = now.AddDays(-7),
                DraftTimeZoneId = "Europe/Istanbul"
            }
        };
        var teamNames = new[] { "Istanbul Owls", "Ankara Foxes", "Izmir Waves", "Bursa Bears" };
        var teams = users.Select((user, index) => new FantasyTeam
        {
            Id = Id(200 + index),
            Name = teamNames[index],
            LeagueId = leagueId,
            OwnerId = user.Id,
            CreatedAt = now.AddDays(-25)
        }).ToArray();
        var firstNames = new[]
        {
            "LeBron", "Stephen", "Nikola", "Luka", "Giannis", "Jayson", "Kevin", "Shai",
            "Anthony", "Joel", "Devin", "Jimmy", "Damian", "Kawhi", "Trae", "Donovan"
        };
        var lastNames = new[]
        {
            "James", "Curry", "Jokic", "Doncic", "Antetokounmpo", "Tatum", "Durant", "Gilgeous-Alexander",
            "Davis", "Embiid", "Booker", "Butler", "Lillard", "Leonard", "Young", "Mitchell"
        };
        var players = firstNames.Select((firstName, index) => new NbaPlayer
        {
            Id = Id(300 + index),
            NbaId = 9000 + index,
            FirstName = firstName,
            LastName = lastNames[index],
            Team = $"T{index + 1:00}",
            Position = index % 3 == 0 ? "F" : index % 3 == 1 ? "G" : "C",
            JerseyNumber = index + 1,
            HeightCm = 190 + index,
            WeightKg = 88 + index,
            SeasonStats =
            [
                new PlayerStats
                {
                    NbaPlayerId = Id(300 + index),
                    Season = 2026,
                    GamesPlayed = 20 + index,
                    GamesStarted = 18 + index,
                    MinutesPerGame = 28 + index / 2d,
                    PointsPerGame = 15 + index,
                    ReboundsPerGame = 4 + index / 3d,
                    AssistsPerGame = 3 + index / 4d
                }
            ]
        }).ToArray();
        var rosters = teams.SelectMany((team, teamIndex) =>
            players.Skip(teamIndex * 3).Take(3).Select(player => new FantasyTeamPlayer
            {
                FantasyTeamId = team.Id,
                LeagueId = leagueId,
                NbaPlayerId = player.Id,
                AcquiredAt = now.AddDays(-6)
            })).ToArray();
        var draftOrder = Enumerable.Range(0, 12).Select(index => new DraftPickOrder
        {
            Id = Id(400 + index),
            LeagueId = leagueId,
            TeamId = teams[index % teams.Length].Id,
            Round = index / teams.Length + 1,
            PositionInRound = index % teams.Length + 1,
            OverallPick = index + 1,
            NbaPlayerId = players[index].Id,
            PickedAt = now.AddDays(-7).AddMinutes(index)
        }).ToArray();
        var fixtures = new[]
        {
            Fixture(leagueId, 1, teams[0], teams[1], now.AddDays(-5), 112, 104),
            Fixture(leagueId, 1, teams[2], teams[3], now.AddDays(-5), 98, 101),
            Fixture(leagueId, 2, teams[0], teams[2], now.AddDays(-2), 108, 108),
            Fixture(leagueId, 2, teams[1], teams[3], now.AddDays(-2), 95, 103),
            Fixture(leagueId, 3, teams[0], teams[3], now.AddDays(2)),
            Fixture(leagueId, 3, teams[1], teams[2], now.AddDays(2))
        };

        await context.AddRangeAsync(users, cancellationToken);
        await context.AddAsync(league, cancellationToken);
        await context.AddRangeAsync(teams, cancellationToken);
        await context.AddRangeAsync(players, cancellationToken);
        await context.AddRangeAsync(rosters, cancellationToken);
        await context.AddRangeAsync(draftOrder, cancellationToken);
        await context.AddRangeAsync(fixtures, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(int key, string username, string email, IPasswordHasher hasher) => new()
    {
        Id = Id(key), Username = username, Email = email,
        Password = hasher.Hash(SeedPassword), TimeZoneId = "Europe/Istanbul"
    };

    private static LeagueFixture Fixture(
        Guid leagueId, int week, FantasyTeam home, FantasyTeam away,
        DateTime gameTime, int? homeScore = null, int? awayScore = null) => new()
    {
        LeagueId = leagueId, Week = week, HomeTeamId = home.Id, AwayTeamId = away.Id,
        GameTime = gameTime, HomeScore = homeScore, AwayScore = awayScore,
        Status = homeScore.HasValue ? MatchStatus.Completed : MatchStatus.Scheduled
    };

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
}
