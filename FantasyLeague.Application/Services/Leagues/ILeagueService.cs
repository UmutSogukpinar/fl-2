using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;

namespace FantasyLeague.Application.Services.Leagues;

public interface ILeagueService
{
    Task<IReadOnlyCollection<LeagueResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LeagueResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeagueResponse> CreateAsync(CreateLeagueRequest request, CancellationToken cancellationToken = default);
    Task<LeagueResponse> UpdateAsync(Guid id, UpdateLeagueRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
