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
    [InlineData(4, 12)]
    [InlineData(5, 20)]
    public void CreateDoubleRoundRobinFixtures_CreatesEachHomeAwayPairOnce(
        int teamCount,
        int expectedFixtureCount)
    {
        var teams = Enumerable.Range(0, teamCount).Select(_ => Guid.NewGuid()).ToArray();

        var fixtures = LeagueSetupGenerator.CreateDoubleRoundRobinFixtures(
            Guid.NewGuid(), teams);

        Assert.Equal(expectedFixtureCount, fixtures.Count);
        Assert.DoesNotContain(fixtures, fixture => fixture.HomeTeamId == fixture.AwayTeamId);
        foreach (var home in teams)
        foreach (var away in teams.Where(team => team != home))
            Assert.Single(fixtures, fixture =>
                fixture.HomeTeamId == home && fixture.AwayTeamId == away);
    }
}
