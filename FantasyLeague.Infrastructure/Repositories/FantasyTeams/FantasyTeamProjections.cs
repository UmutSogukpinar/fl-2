using System.Linq.Expressions;

using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.Repositories.Projections;

internal static class FantasyTeamProjections
{
    internal static readonly Expression<Func<FantasyTeam, FantasyTeamResponse>> Response =
        team => new FantasyTeamResponse(
            team.Id,
            team.Name,
            team.LeagueId,
            team.OwnerId,
            team.CreatedAt,
            team.UpdatedAt);
}
