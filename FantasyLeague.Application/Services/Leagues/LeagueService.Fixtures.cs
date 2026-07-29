namespace FantasyLeague.Application.Services.Leagues;

using FantasyLeague.Domain.Enums;

public sealed partial class LeagueService
{
    public async Task<int> ProcessDueFixturesAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var fixtures = await _leagueSetupRepository.GetDueFixturesAsync(utcNow, cancellationToken);
        foreach (var fixture in fixtures)
        {
            var league = await _leagueRepository.GetResponseByIdAsync(fixture.LeagueId, cancellationToken);
            if (league is null) continue;

            var stats = await _playerRepository.GetMatchStatsByTeamIdsAsync(
                fixture.LeagueId, fixture.HomeTeamId, fixture.AwayTeamId,
                league.Season, cancellationToken);
            fixture.HomeScore = Score(stats.HomeTeamStats.PointsPerGame);
            fixture.AwayScore = Score(stats.AwayTeamStats.PointsPerGame);
            if (fixture.HomeScore == fixture.AwayScore) fixture.HomeScore++;
        }

        if (fixtures.Count > 0)
        {
            await _leagueSetupRepository.SaveChangesAsync(cancellationToken);

            foreach (var leagueId in fixtures.Select(fixture => fixture.LeagueId).Distinct())
            {
                if (await _leagueSetupRepository.HasUnfinishedFixturesAsync(
                    leagueId, cancellationToken)) continue;

                var league = await _leagueRepository.GetTrackedByIdAsync(
                    leagueId, cancellationToken);
                if (league is null || league.Status == LeagueStatus.Completed) continue;

                league.Status = LeagueStatus.Completed;
                league.UpdatedAt = utcNow;
                await _leagueRepository.SaveChangesAsync(cancellationToken);
            }
        }

        return fixtures.Count;
    }

    private static int Score(double projectedPoints) =>
        Math.Max(0, (int)Math.Round(projectedPoints, MidpointRounding.AwayFromZero));
}
