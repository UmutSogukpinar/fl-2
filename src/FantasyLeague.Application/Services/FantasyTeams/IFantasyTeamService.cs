using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;

namespace FantasyLeague.Application.Services.FantasyTeams;

public interface IFantasyTeamService
{
    Task<PagedResponse<FantasyTeamResponse>> GetByLeagueIdAsync(Guid leagueId, PaginationRequest request, CancellationToken cancellation = default);
    Task<FantasyTeamResponse> AddLeagueMemberAsync(Guid leagueId, AddLeagueMemberRequest request, CancellationToken cancellation = default);
    Task<FantasyTeamResponse> JoinLeagueAsync(JoinLeagueRequest request, CancellationToken cancellation = default);
    Task RemoveLeagueMemberAsync(Guid leagueId, Guid teamId, CancellationToken cancellation = default);
    Task<FantasyTeamResponse> GetByIdAsync(Guid id, CancellationToken cancellation = default);
    Task<FantasyTeamResponse> UpdateAsync(Guid id, UpdateFantasyTeamRequest request, CancellationToken cancellation = default);
    Task DeleteAsync(Guid id, CancellationToken cancellation = default);

    Task ReleaseAPlayerAsync(
        Guid id, Guid playerId,
        CancellationToken cancellation = default
    );
    Task AddPlayerFromPoolAsync(
        Guid teamId, Guid playerId,
        CancellationToken cancellation = default
    );

    Task<Guid> CreateTransferAsync(
        Guid initiatingTeamId,
        CreateTransferRequest request,
        CancellationToken cancellation = default
    );
    Task ApproveTransferAsync(Guid transferId, Guid approvingTeamId, CancellationToken cancellation = default);
    Task<IReadOnlyCollection<TeamRosterPlayerResponse>> GetRosterPlayersAsync(
        Guid teamId, CancellationToken cancellation = default);
    Task<PagedResponse<TeamRosterPlayerResponse>> GetPlayerPoolAsync(
        Guid teamId,
        PaginationRequest request,
        CancellationToken cancellation = default);
    Task<IReadOnlyCollection<TransferResponse>> GetTransfersAsync(
        Guid teamId, CancellationToken cancellation = default);
}
