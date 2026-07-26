using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using System.Security.Cryptography;

namespace FantasyLeague.Application.Mappings;

public static class LeagueMappings
{
    public static League ToEntity(this CreateLeagueRequest request) => new()
    {
        Name = request.Name.Trim(),
        Description = NormalizeDescription(request.Description),
        Season = request.Season,
        MaxTeams = request.MaxTeams,
        CommissionerId = request.CommissionerId,
        Status = LeagueStatus.Created,
        DraftDate = request.DraftDate,
        JoinCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(4))
    };

    public static void MapTo(this UpdateLeagueRequest request, League league)
    {
        league.Name = request.Name.Trim();
        league.Description = NormalizeDescription(request.Description);
        league.MaxTeams = request.MaxTeams;
        league.DraftDate = request.DraftDate;
        league.UpdatedAt = DateTime.UtcNow;
    }

    public static LeagueResponse ToResponse(this League league) => new(
        league.Id,
        league.Name,
        league.Description,
        league.Season,
        league.MaxTeams,
        league.CommissionerId,
        league.Status,
        league.DraftDate,
        league.JoinCode,
        league.CreatedAt,
        league.UpdatedAt);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
