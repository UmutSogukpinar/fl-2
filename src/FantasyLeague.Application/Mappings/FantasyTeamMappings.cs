using FantasyLeague.Domain.Entities.FantasyTeams;

using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Mappings;

public static class FantasyTeamMappings
{
    public static FantasyTeam ToEntity(this CreateFantasyTeamRequest request)
    {
        return new FantasyTeam
        {
            Name = request.Name.Trim(),
            LeagueId = request.LeagueId,
            OwnerId = request.OwnerId
        };
    }

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
