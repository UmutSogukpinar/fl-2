using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.DTOs.Responses.NbaPlayers;

public sealed record NbaPlayerBasicResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Team,
    string Position
) : IPlayerResponse;

public sealed record NbaPlayerDetailedResponse(
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
    DateTime? UpdatedAt
) : IPlayerResponse;

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
    IEnumerable<PlayerStatsResponse> SeasonStats
) : IPlayerResponse;
