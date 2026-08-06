using FantasyLeague.Application.DTOs.Requests.Drafts;
using FantasyLeague.Application.DTOs.Responses.Drafts;

namespace FantasyLeague.Application.Services.Drafts;

public interface IDraftService
{
    Task<DraftStateResponse> GetStateAsync(Guid leagueId, CancellationToken cancellation = default);
    Task<IReadOnlyList<DraftStateResponse>> StartDueDraftsAsync(DateTime utcNow, CancellationToken cancellation = default);
    Task<IReadOnlyList<DraftStateResponse>> AutoPickExpiredAsync(DateTime utcNow, CancellationToken cancellation = default);
    Task<DraftStateResponse> CloseDelayedLeagueAsync(Guid leagueId, Guid commissionerId, CancellationToken cancellation = default);
    Task<DraftStateResponse> MakePickAsync(Guid leagueId, MakeDraftPickRequest request, CancellationToken cancellation = default);
}
