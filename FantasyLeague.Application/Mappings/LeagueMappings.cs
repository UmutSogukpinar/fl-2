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
        Settings = new LeagueSettings
        {
            DraftDate = request.DraftDate,
            RosterSize = request.RosterSize
        },
        JoinCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(4))
    };

    public static void MapTo(this UpdateLeagueRequest request, League league)
    {
        league.Name = request.Name.Trim();
        league.Description = NormalizeDescription(request.Description);
        league.MaxTeams = request.MaxTeams;
        league.Settings.DraftDate = request.DraftDate;
        league.Settings.RosterSize = request.RosterSize;
        league.Settings.UpdatedAt = DateTime.UtcNow;
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
        league.Settings.DraftDate,
        league.JoinCode,
        league.CreatedAt,
        league.UpdatedAt,
        league.Settings.RosterSize);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
