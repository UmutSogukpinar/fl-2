using FantasyLeague.Application.Services.Leagues;

namespace FantasyLeague.Application.Tests;

public sealed class LeagueSetupGeneratorTests
{
    [Fact]
    public void CreateSnakeDraftOrder_ReversesEveryEvenRound()
    {
        var leagueId = Guid.NewGuid();
        var teams = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();

        var picks = LeagueSetupGenerator.CreateSnakeDraftOrder(leagueId, teams, 3);

        Assert.Equal(teams, picks.Where(pick => pick.Round == 1).Select(pick => pick.TeamId));
        Assert.Equal(teams.Reverse(), picks.Where(pick => pick.Round == 2).Select(pick => pick.TeamId));
        Assert.Equal(teams, picks.Where(pick => pick.Round == 3).Select(pick => pick.TeamId));
        Assert.Equal(Enumerable.Range(1, 12), picks.Select(pick => pick.OverallPick));
    }

    [Theory]
    [InlineData(4, 6)]
    [InlineData(5, 10)]
    public void CreateRoundRobinFixtures_CreatesEachTeamPairOnce(
        int teamCount,
        int expectedFixtureCount)
    {
        var teams = Enumerable.Range(0, teamCount).Select(_ => Guid.NewGuid()).ToArray();

        var fixtures = LeagueSetupGenerator.CreateRoundRobinFixtures(
            Guid.NewGuid(), teams);

        Assert.Equal(expectedFixtureCount, fixtures.Count);
        Assert.DoesNotContain(fixtures, fixture => fixture.HomeTeamId == fixture.AwayTeamId);
        for (var first = 0; first < teams.Length; first++)
        for (var second = first + 1; second < teams.Length; second++)
        {
            Assert.Single(fixtures, fixture =>
                fixture.HomeTeamId == teams[first] && fixture.AwayTeamId == teams[second]
                || fixture.HomeTeamId == teams[second] && fixture.AwayTeamId == teams[first]);
        }
    }

    [Fact]
    public void CreateRoundRobinFixtures_SchedulesDemoRoundsEveryFiveMinutes()
    {
        var completedAt = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        var teams = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();

        var fixtures = LeagueSetupGenerator.CreateRoundRobinFixtures(
            Guid.NewGuid(), teams, completedAt, TimeSpan.FromMinutes(5));

        Assert.All(fixtures.Where(fixture => fixture.Week == 1),
            fixture => Assert.Equal(completedAt.AddMinutes(5), fixture.GameTime));
        Assert.All(fixtures.Where(fixture => fixture.Week == 2),
            fixture => Assert.Equal(completedAt.AddMinutes(10), fixture.GameTime));
        Assert.All(fixtures.Where(fixture => fixture.Week == 3),
            fixture => Assert.Equal(completedAt.AddMinutes(15), fixture.GameTime));
    }
}
