using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Users;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Time;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Mappings;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService
{
    public async Task<LeagueResponse> UpdateAsync(
        Guid id,
        UpdateLeagueRequest request,
        CancellationToken cancellation = default)
    {
        request = request.NormalizeUpdateLeagueRequest();
        request.ValidateUpdateLeagueRequest();

        await EnsureMaxTeamsCanBeUpdatedAsync(
            id,
            request.MaxTeams,
            cancellation);

        var league = await GetTrackedLeagueOrThrowAsync(id, cancellation);
        var commissioner = await _userRepository.GetResponseByIdAsync(
            league.CommissionerId,
            cancellation)
            ?? throw new NotFoundException(
                $"User '{league.CommissionerId}' was not found.");

        var draftDateUtc = DateTimeUtcConverter.ConvertToUtc(
            request.DraftDate,
            commissioner.TimeZoneId);
        draftDateUtc!.Value.ValidateFutureDraftDate();

        request.MapTo(league, draftDateUtc, commissioner.TimeZoneId);
        await _leagueRepository.SaveChangesAsync(cancellation);

        return league.ToResponse();
    }

    private async Task EnsureMaxTeamsCanBeUpdatedAsync(
        Guid leagueId,
        int maxTeams,
        CancellationToken cancellation)
    {
        var currentTeamCount = await _teamRepository.CountByLeagueIdAsync(
            leagueId,
            cancellation);

        if (maxTeams < currentTeamCount)
        {
            throw new ConflictException(
                $"MaxTeams cannot be lower than " +
                $"the current team count ({currentTeamCount}).");
        }
    }
}
