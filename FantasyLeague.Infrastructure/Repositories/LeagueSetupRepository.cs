using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class LeagueSetupRepository(AppDbContext dbContext) : ILeagueSetupRepository
{
    public Task<bool> ExistsAsync(Guid leagueId, CancellationToken cancellationToken) =>
        dbContext.Set<DraftPickOrder>().AnyAsync(pick => pick.LeagueId == leagueId, cancellationToken);

    public async Task AddAsync(
        IReadOnlyCollection<LeagueFixture> fixtures,
        IReadOnlyCollection<DraftPickOrder> draftOrder,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<LeagueFixture>().AddRangeAsync(fixtures, cancellationToken);
        await dbContext.Set<DraftPickOrder>().AddRangeAsync(draftOrder, cancellationToken);
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
                (item, away) => new LeagueFixtureResponse(
                    item.fixture.Id, item.fixture.LeagueId, item.fixture.Week,
                    item.home.Id, item.home.Name, away.Id, away.Name))
            .OrderBy(fixture => fixture.Week)
            .ThenBy(fixture => fixture.HomeTeamName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(
        Guid leagueId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<DraftPickOrder>()
            .AsNoTracking()
            .Where(pick => pick.LeagueId == leagueId)
            .Join(dbContext.Set<FantasyTeam>(), pick => pick.TeamId, team => team.Id,
                (pick, team) => new DraftPickOrderResponse(
                    pick.Id, pick.LeagueId, team.Id, team.Name,
                    pick.Round, pick.PositionInRound, pick.OverallPick))
            .OrderBy(pick => pick.OverallPick)
            .ToListAsync(cancellationToken);
}
