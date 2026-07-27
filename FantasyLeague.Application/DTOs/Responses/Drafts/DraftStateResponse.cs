using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.DTOs.Responses.Drafts;

public sealed record DraftStateResponse(
    Guid LeagueId,
    LeagueStatus Status,
    int CompletedPicks,
    int TotalPicks,
    DraftPickResponse? CurrentPick,
    IReadOnlyList<DraftPickResponse> Picks);
