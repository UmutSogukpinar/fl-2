using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface ILeagueSetupRepository
{
    Task<bool> ExistsAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(Guid leagueId, CancellationToken cancellationToken);
    Task AddAsync(
        IReadOnlyCollection<LeagueFixture> fixtures,
        IReadOnlyCollection<DraftPickOrder> draftOrder,
        CancellationToken cancellationToken);
}
