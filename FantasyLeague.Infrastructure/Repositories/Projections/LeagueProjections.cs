using System.Linq.Expressions;

using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.Repositories.Projections;

internal static class LeagueProjections
{
    internal static readonly Expression<Func<League, LeagueResponse>> Response =
        league => new LeagueResponse(
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
            league.Settings.RosterSize,
            league.Settings.DraftTimeZoneId);
}
