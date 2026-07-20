using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;

namespace FantasyLeague.Application.Services.FantasyTeams;

public interface IFantasyTeamService
{
    Task<IReadOnlyCollection<FantasyTeamResponse>> GetByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default);

    Task<FantasyTeamResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FantasyTeamResponse> CreateAsync(
        CreateFantasyTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<FantasyTeamResponse> UpdateAsync(
        Guid id,
        UpdateFantasyTeamRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
