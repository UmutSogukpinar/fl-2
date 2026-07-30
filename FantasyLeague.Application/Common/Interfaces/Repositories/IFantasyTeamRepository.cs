using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IFantasyTeamRepository
{
    Task<(IReadOnlyCollection<FantasyTeamResponse> Items, int TotalCount)> GetPagedByLeagueIdAsync(Guid leagueId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<FantasyTeamResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<FantasyTeam?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> CountByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetIdsByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken
    );
    Task<FastasyTeamConflictResult> ExistsAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellationToken
    );
    Task AddAsync(
        FantasyTeam team,
        CancellationToken cancellationToken
    );

    void Remove(FantasyTeam team);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    Task ReleaseAPlayerAsync(
        Guid id, Guid playerId,
        CancellationToken cancellation
    );


    public Task<TradeValidationResult>
    ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
        Guid? homeId = null,
        Guid? awayId = null,
        Guid? playerId = null,
        CancellationToken cancellation = default
    );

}
