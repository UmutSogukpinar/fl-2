using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Mappings;

public static class LeagueMappings
{
    public static League ToEntity(this CreateLeagueRequest request, User commissioner) => new()
    {
        Name = request.Name.Trim(),
        Description = NormalizeDescription(request.Description),
        Season = request.Season,
        MaxTeams = request.MaxTeams,
        CommissionerId = commissioner.Id,
        Commissioner = commissioner
    };

    public static void MapTo(this UpdateLeagueRequest request, League league)
    {
        league.Name = request.Name.Trim();
        league.Description = NormalizeDescription(request.Description);
        league.MaxTeams = request.MaxTeams;
        league.UpdatedAt = DateTime.UtcNow;
    }

    public static LeagueResponse ToResponse(this League league) => new(
        league.Id,
        league.Name,
        league.Description,
        league.Season,
        league.MaxTeams,
        league.CommissionerId,
        league.CreatedAt,
        league.UpdatedAt);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
