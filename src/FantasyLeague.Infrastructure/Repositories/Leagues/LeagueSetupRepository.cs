using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.Leagues;

public sealed class LeagueSetupRepository(AppDbContext dbContext) : ILeagueSetupRepository
{
    public Task<bool> DraftOrderExistsAsync(Guid leagueId, CancellationToken cancellation)
    {
        return dbContext.Set<DraftPickOrder>()
        .AnyAsync(
            pick => pick.LeagueId == leagueId,
            cancellation
        );
    }

    public Task AddDraftOrderAsync(
        IReadOnlyCollection<DraftPickOrder> draftOrder,
        CancellationToken cancellation)
    {
        return dbContext.Set<DraftPickOrder>()
            .AddRangeAsync(draftOrder, cancellation);
    }

    public Task AddFixturesAsync(
        IReadOnlyCollection<LeagueFixture> fixtures,
        CancellationToken cancellation)
    {
        return dbContext.Set<LeagueFixture>()
            .AddRangeAsync(fixtures, cancellation);
    }

    public async Task<IReadOnlyList<LeagueFixture>> GetDueFixturesAsync(
        DateTime utcNow, CancellationToken cancellation)
    {
        return await dbContext.Set<LeagueFixture>()
                        .Where(fixture => fixture.GameTime <= utcNow)
                        .Where(fixture =>
                            fixture.Status == MatchStatus.Scheduled ||
                            (fixture.Status == MatchStatus.Completed &&
                             fixture.HomeScore == 0 &&
                             fixture.AwayScore == 0))
                        .OrderBy(fixture => fixture.GameTime)
                        .ThenBy(fixture => fixture.Id)
                        .ToListAsync(cancellation);
    }

    public Task SaveChangesAsync(CancellationToken cancellation)
    {
        return dbContext.SaveChangesAsync(cancellation);
    }

    public Task<bool> HasUnfinishedFixturesAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        return dbContext.Set<LeagueFixture>().AnyAsync(
            fixture => fixture.LeagueId == leagueId
                && fixture.Status != MatchStatus.Completed
                && fixture.Status != MatchStatus.Cancelled,
            cancellation);
    }

    public async Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        return await dbContext.Set<LeagueFixture>()
            .AsNoTracking()
            .Where(fixture => fixture.LeagueId == leagueId)
            .Join(dbContext.Set<FantasyTeam>(), fixture => fixture.HomeTeamId, team => team.Id,
                (fixture, home) => new { fixture, home })
            .Join(dbContext.Set<FantasyTeam>(), item => item.fixture.AwayTeamId, team => team.Id,
                (item, away) => new { item.fixture, item.home, away })
            .OrderBy(item => item.fixture.Week)
            .ThenBy(item => item.home.Name)
            .Select(item => new LeagueFixtureResponse(
                item.fixture.Id,
                item.fixture.LeagueId,
                item.fixture.Week,
                item.home.Id,
                item.home.Name,
                item.away.Id,
                item.away.Name,
                item.fixture.HomeScore,
                item.fixture.AwayScore,
                item.fixture.GameTime,
                item.fixture.Status.ToString()))
            .ToListAsync(cancellation);
    }

    public async Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        return await dbContext.Set<DraftPickOrder>()
            .AsNoTracking()
            .Where(pick => pick.LeagueId == leagueId)
            .Join(dbContext.Set<FantasyTeam>(), pick => pick.TeamId, team => team.Id,
                (pick, team) => new { pick, team })
            .OrderBy(item => item.pick.OverallPick)
            .Select(item => new DraftPickOrderResponse(
                item.pick.Id,
                item.pick.LeagueId,
                item.team.Id,
                item.team.Name,
                item.pick.Round,
                item.pick.PositionInRound,
                item.pick.OverallPick))
            .ToListAsync(cancellation);
    }

    public async Task<IReadOnlyList<LeagueStandingResponse>> GetStandingsAsync(
        Guid leagueId,
        CancellationToken cancellation)
    {
        var teams = await dbContext.Set<FantasyTeam>().AsNoTracking()
            .Where(team => team.LeagueId == leagueId)
            .Select(team => new { team.Id, team.Name })
            .ToArrayAsync(cancellation);

        var fixtures = await dbContext.Set<LeagueFixture>().AsNoTracking()
            .Where(fixture => fixture.LeagueId == leagueId
                && fixture.Status == MatchStatus.Completed
                && fixture.HomeScore != null && fixture.AwayScore != null)
            .Select(fixture => new
            {
                fixture.HomeTeamId,
                fixture.AwayTeamId,
                HomeScore = fixture.HomeScore!.Value,
                AwayScore = fixture.AwayScore!.Value
            })
            .ToArrayAsync(cancellation);

        var rows = teams.Select(team =>
        {
            var home = fixtures.Where(game => game.HomeTeamId == team.Id).ToArray();
            var away = fixtures.Where(game => game.AwayTeamId == team.Id).ToArray();
            var won = home.Count(game => game.HomeScore > game.AwayScore)
                + away.Count(game => game.AwayScore > game.HomeScore);
            var drawn = home.Count(game => game.HomeScore == game.AwayScore)
                + away.Count(game => game.AwayScore == game.HomeScore);
            var played = home.Length + away.Length;
            var pointsFor = home.Sum(game => game.HomeScore) + away.Sum(game => game.AwayScore);
            var pointsAgainst = home.Sum(game => game.AwayScore) + away.Sum(game => game.HomeScore);
            return new
            {
                team.Id,
                team.Name,
                Played = played,
                Won = won,
                Drawn = drawn,
                Lost = played - won - drawn,
                PointsFor = pointsFor,
                PointsAgainst = pointsAgainst,
                Difference = pointsFor - pointsAgainst,
                Points = won * 3 + drawn
            };
        })
        .OrderByDescending(row => row.Points)
        .ThenByDescending(row => row.Difference)
        .ThenByDescending(row => row.PointsFor)
        .ThenBy(row => row.Name)
        .ToArray();

        return rows.Select((row, index) => new LeagueStandingResponse(
            index + 1, row.Id, row.Name, row.Played, row.Won, row.Drawn,
            row.Lost, row.PointsFor, row.PointsAgainst, row.Difference,
            row.Points)).ToArray();
    }
}
