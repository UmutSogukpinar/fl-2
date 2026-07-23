using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Mappings;

public static class FantasyTeamMappings
{
    public static FantasyTeam ToEntity(
        this CreateFantasyTeamRequest request,
        League league,
        User owner) => new()
    {
        Name = request.Name.Trim(),
        LeagueId = league.Id,
        OwnerId = owner.Id,
        Owner = owner
    };

    public static void MapTo(this UpdateFantasyTeamRequest request, FantasyTeam team)
    {
        team.Name = request.Name.Trim();
        team.UpdatedAt = DateTime.UtcNow;
    }

    public static FantasyTeamResponse ToResponse(this FantasyTeam team) => new(
        team.Id,
        team.Name,
        team.LeagueId,
        team.OwnerId,
        team.CreatedAt,
        team.UpdatedAt);
}
