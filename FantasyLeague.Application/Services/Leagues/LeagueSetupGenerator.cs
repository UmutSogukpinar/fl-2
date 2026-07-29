using System.Security.Cryptography;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.Leagues;

public static class LeagueSetupGenerator
{
    public static Guid[] CreateRandomTeamOrder(IEnumerable<Guid> teamIds)
    {
        var teams = teamIds.ToArray();
        RandomNumberGenerator.Shuffle(teams);
        return teams;
    }

    public static IReadOnlyList<DraftPickOrder> CreateSnakeDraftOrder(
        Guid leagueId,
        IReadOnlyList<Guid> teams,
        int rounds)
    {
        var picks = new List<DraftPickOrder>(teams.Count * rounds);
        for (var round = 1; round <= rounds; round++)
        {
            for (var position = 1; position <= teams.Count; position++)
            {
                var teamIndex = round % 2 == 1
                    ? position - 1
                    : teams.Count - position;

                picks.Add(new DraftPickOrder
                {
                    LeagueId = leagueId,
                    TeamId = teams[teamIndex],
                    Round = round,
                    PositionInRound = position,
                    OverallPick = ((round - 1) * teams.Count) + position
                });
            }
        }

        return picks;
    }

    public static IReadOnlyList<LeagueFixture> CreateRoundRobinFixtures(
        Guid leagueId,
        IReadOnlyList<Guid> teamIds,
        DateTime? draftCompletedAt = null,
        TimeSpan? roundInterval = null)
    {
        var rotation = teamIds.Cast<Guid?>().ToList();
        if (rotation.Count % 2 != 0) rotation.Add(null);

        var fixtures = new List<LeagueFixture>();
        var rounds = rotation.Count - 1;
        var matchesPerRound = rotation.Count / 2;

        for (var round = 0; round < rounds; round++)
        {
            for (var match = 0; match < matchesPerRound; match++)
            {
                var first = rotation[match];
                var second = rotation[rotation.Count - 1 - match];
                if (!first.HasValue || !second.HasValue) continue;

                var swapHome = (round + match) % 2 == 1;
                fixtures.Add(new LeagueFixture
                {
                    LeagueId = leagueId,
                    Week = round + 1,
                    HomeTeamId = swapHome ? second.Value : first.Value,
                    AwayTeamId = swapHome ? first.Value : second.Value,
                    GameTime = draftCompletedAt?.Add(
                        (roundInterval ?? TimeSpan.FromDays(7)) * (round + 1))
                });
            }

            var last = rotation[^1];
            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(1, last);
        }

        return fixtures;
    }
}
