using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class LeagueSetupRepository(AppDbContext dbContext) : ILeagueSetupRepository
{
    public Task<bool> DraftOrderExistsAsync(Guid leagueId, CancellationToken cancellationToken) =>
        dbContext.Set<DraftPickOrder>().AnyAsync(pick => pick.LeagueId == leagueId, cancellationToken);

    public Task AddDraftOrderAsync(
        IReadOnlyCollection<DraftPickOrder> draftOrder,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<DraftPickOrder>()
            .AddRangeAsync(draftOrder, cancellationToken);
    }

    public Task AddFixturesAsync(
        IReadOnlyCollection<LeagueFixture> fixtures,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<LeagueFixture>()
            .AddRangeAsync(fixtures, cancellationToken);
    }

    public async Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(
        Guid leagueId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<LeagueFixture>()
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
                item.away.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(
        Guid leagueId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<DraftPickOrder>()
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
            .ToListAsync(cancellationToken);
}
