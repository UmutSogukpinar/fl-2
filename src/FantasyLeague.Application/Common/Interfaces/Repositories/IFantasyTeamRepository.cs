using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IFantasyTeamRepository
{
    Task<(IReadOnlyCollection<FantasyTeamResponse> Items, int TotalCount)> GetPagedByLeagueIdAsync(Guid leagueId, PaginationRequest request, CancellationToken cancellationToken);
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
