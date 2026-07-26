namespace FantasyLeague.Application.DTOs.Responses.NbaPlayers;

public sealed record NbaPlayerExtendedResponse(
    Guid Id,
    int NbaId,
    string FirstName,
    string LastName,
    string? Team,
    string? Position,
    int? JerseyNumber,
    int? HeightCm,
    decimal? WeightKg,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    PlayerStatsResponse? SeasonStats
) : IPlayerResponse;
