using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface ILeagueSetupRepository
{
    Task<bool> DraftOrderExistsAsync(Guid leagueId, CancellationToken cancellation);
    Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(Guid leagueId, CancellationToken cancellation);
    Task<IReadOnlyList<LeagueStandingResponse>> GetStandingsAsync(Guid leagueId, CancellationToken cancellation);
    Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(Guid leagueId, CancellationToken cancellation);
    Task AddDraftOrderAsync(
        IReadOnlyCollection<DraftPickOrder> draftOrder,
        CancellationToken cancellation);
    Task AddFixturesAsync(
        IReadOnlyCollection<LeagueFixture> fixtures,
        CancellationToken cancellation);
    Task<IReadOnlyList<LeagueFixture>> GetDueFixturesAsync(DateTime utcNow, CancellationToken cancellation);
    Task<bool> HasUnfinishedFixturesAsync(Guid leagueId, CancellationToken cancellation);
    Task SaveChangesAsync(CancellationToken cancellation);
}
