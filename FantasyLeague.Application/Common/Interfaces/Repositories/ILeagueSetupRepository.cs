using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface ILeagueSetupRepository
{
    Task<bool> DraftOrderExistsAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeagueStandingResponse>> GetStandingsAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(Guid leagueId, CancellationToken cancellationToken);
    Task AddDraftOrderAsync(
        IReadOnlyCollection<DraftPickOrder> draftOrder,
        CancellationToken cancellationToken);
    Task AddFixturesAsync(
        IReadOnlyCollection<LeagueFixture> fixtures,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<LeagueFixture>> GetDueFixturesAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<bool> HasUnfinishedFixturesAsync(Guid leagueId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
