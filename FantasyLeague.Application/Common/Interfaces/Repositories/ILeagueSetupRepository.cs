using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface ILeagueSetupRepository
{
    Task<bool> DraftOrderExistsAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(Guid leagueId, CancellationToken cancellationToken);
    Task AddDraftOrderAsync(
        IReadOnlyCollection<DraftPickOrder> draftOrder,
        CancellationToken cancellationToken);
    Task AddFixturesAsync(
        IReadOnlyCollection<LeagueFixture> fixtures,
        CancellationToken cancellationToken);
}
