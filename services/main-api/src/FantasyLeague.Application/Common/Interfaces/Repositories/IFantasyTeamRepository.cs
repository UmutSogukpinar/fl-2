using FantasyLeague.Domain.Entities.FantasyTeams;

using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IFantasyTeamRepository
{
    Task<(IReadOnlyCollection<FantasyTeamResponse> Items, int TotalCount)> GetPagedByLeagueIdAsync(Guid leagueId, PaginationRequest request, CancellationToken cancellation);
    Task<FantasyTeamResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellation);
    Task<FantasyTeam?> GetTrackedByIdAsync(Guid id, CancellationToken cancellation);
    Task<int> CountByLeagueIdAsync(Guid leagueId, CancellationToken cancellation);
    Task<IReadOnlyList<Guid>> GetIdsByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellation
    );
    Task<FastasyTeamConflictResult> ExistsAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellation
    );
    Task AddAsync(
        FantasyTeam team,
        CancellationToken cancellation
    );

    void Remove(FantasyTeam team);
    Task SaveChangesAsync(CancellationToken cancellation);

    Task ReleaseAPlayerAsync(
        Guid id, Guid playerId,
        CancellationToken cancellation
    );
    Task AddPlayerFromPoolAsync(
        Guid teamId, Guid playerId,
        CancellationToken cancellation
    );

    Task<(int PlayerCount, int RosterSize)> GetRosterStateAsync(
        Guid teamId,
        CancellationToken cancellation
    );

    Task<Guid> CreateTransferAsync(
        Guid initiatingTeamId,
        Guid counterpartyTeamId,
        IReadOnlyCollection<Guid> offeredPlayerIds,
        IReadOnlyCollection<Guid> requestedPlayerIds,
        CancellationToken cancellation
    );
    Task ApproveTransferAsync(Guid transferId, Guid approvingTeamId, CancellationToken cancellation);
    Task<IReadOnlyCollection<TeamRosterPlayerResponse>> GetRosterPlayersAsync(
        Guid teamId, CancellationToken cancellation);
    Task<(IReadOnlyCollection<TeamRosterPlayerResponse> Items, int TotalCount)>
        GetPlayerPoolAsync(
            Guid teamId,
            PaginationRequest request,
            CancellationToken cancellation);
    Task<IReadOnlyCollection<TransferResponse>> GetTransfersAsync(
        Guid teamId, CancellationToken cancellation);


    public Task<TradeValidationResult>
    ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
        Guid? homeId = null,
        Guid? awayId = null,
        Guid? playerId = null,
        CancellationToken cancellation = default
    );

}
