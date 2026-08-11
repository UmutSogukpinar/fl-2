using FantasyLeague.Application.Services.Leagues;

namespace FantasyLeague.Application.Tests.Services.Leagues;

public sealed class LeagueSetupGeneratorEdgeCaseTests
{
    [Fact]
    public void CreateSnakeDraftOrder_WithZeroRounds_ReturnsEmpty()
    {
        var picks = LeagueSetupGenerator.CreateSnakeDraftOrder(
            Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], 0);

        Assert.Empty(picks);
    }

    [Fact]
    public void CreateSnakeDraftOrder_WithNegativeRounds_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LeagueSetupGenerator.CreateSnakeDraftOrder(
                Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], -1));
    }

    [Fact]
    public void CreateSnakeDraftOrder_WithNoTeams_ReturnsEmpty()
    {
        Assert.Empty(LeagueSetupGenerator.CreateSnakeDraftOrder(
            Guid.NewGuid(), [], 10));
    }

    [Fact]
    public void CreateSnakeDraftOrder_WithOneTeam_CreatesOnePickPerRound()
    {
        var teamId = Guid.NewGuid();

        var picks = LeagueSetupGenerator.CreateSnakeDraftOrder(
            Guid.NewGuid(), [teamId], 4);

        Assert.Equal(4, picks.Count);
        Assert.All(picks, pick => Assert.Equal(teamId, pick.TeamId));
        Assert.Equal([1, 2, 3, 4], picks.Select(pick => pick.OverallPick));
    }

    [Fact]
    public void CreateRandomTeamOrder_DoesNotMutateInputAndPreservesEveryTeam()
    {
        var teams = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();
        var original = teams.ToArray();

        var shuffled = LeagueSetupGenerator.CreateRandomTeamOrder(teams);

        Assert.Equal(original, teams);
        Assert.Equal(original.Order(), shuffled.Order());
    }

    [Fact]
    public void CreateRandomTeamOrder_WithEmptyInput_ReturnsEmpty()
    {
        Assert.Empty(LeagueSetupGenerator.CreateRandomTeamOrder([]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CreateRoundRobinFixtures_WithFewerThanTwoTeams_ReturnsEmpty(int teamCount)
    {
        var teams = Enumerable.Range(0, teamCount).Select(_ => Guid.NewGuid()).ToArray();

        Assert.Empty(LeagueSetupGenerator.CreateRoundRobinFixtures(Guid.NewGuid(), teams));
    }

    [Fact]
    public void CreateRoundRobinFixtures_WithTwoTeams_CreatesSingleFixture()
    {
        var teams = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var fixture = Assert.Single(LeagueSetupGenerator.CreateRoundRobinFixtures(
            Guid.NewGuid(), teams));

        Assert.Contains(fixture.HomeTeamId, teams);
        Assert.Contains(fixture.AwayTeamId, teams);
        Assert.NotEqual(fixture.HomeTeamId, fixture.AwayTeamId);
    }

    [Fact]
    public void CreateRoundRobinFixtures_WithoutCompletionTime_LeavesGameTimesNull()
    {
        var teams = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();

        var fixtures = LeagueSetupGenerator.CreateRoundRobinFixtures(Guid.NewGuid(), teams);

        Assert.All(fixtures, fixture => Assert.Null(fixture.GameTime));
    }

    [Fact]
    public void CreateRoundRobinFixtures_WithZeroInterval_SchedulesAllAtCompletionTime()
    {
        var completedAt = new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        var teams = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();

        var fixtures = LeagueSetupGenerator.CreateRoundRobinFixtures(
            Guid.NewGuid(), teams, completedAt, TimeSpan.Zero);

        Assert.All(fixtures, fixture => Assert.Equal(completedAt, fixture.GameTime));
    }

    [Fact]
    public void CreateRoundRobinFixtures_WithOddTeamCount_GivesOneByePerRound()
    {
        var teams = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        var fixtures = LeagueSetupGenerator.CreateRoundRobinFixtures(Guid.NewGuid(), teams);

        Assert.Equal(5, fixtures.Select(fixture => fixture.Week).Distinct().Count());
        Assert.All(fixtures.GroupBy(fixture => fixture.Week), round =>
            Assert.Equal(4, round.SelectMany(fixture =>
                new[] { fixture.HomeTeamId, fixture.AwayTeamId }).Distinct().Count()));
    }
}
