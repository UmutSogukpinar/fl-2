namespace FantasyLeague.Application.DTOs.Requests.Drafts;

public sealed record MakeDraftPickRequest(Guid TeamId, Guid OwnerId, Guid NbaPlayerId);
