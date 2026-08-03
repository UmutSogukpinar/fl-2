using FantasyLeague.Application.DTOs.Requests.Drafts;
using FantasyLeague.Application.DTOs.Responses.Drafts;

namespace FantasyLeague.Application.Services.Drafts;

public interface IDraftService
{
    Task<DraftStateResponse> GetStateAsync(Guid leagueId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DraftStateResponse>> StartDueDraftsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DraftStateResponse>> AutoPickExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<DraftStateResponse> CloseDelayedLeagueAsync(Guid leagueId, Guid commissionerId, CancellationToken cancellationToken = default);
    Task<DraftStateResponse> MakePickAsync(Guid leagueId, MakeDraftPickRequest request, CancellationToken cancellationToken = default);
}
