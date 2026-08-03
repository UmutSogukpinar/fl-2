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
        CancellationToken cancellationToken = default)
    {
        request = request.NormalizeUpdateLeagueRequest();
        request.ValidateUpdateLeagueRequest();

        await EnsureMaxTeamsCanBeUpdatedAsync(
            id,
            request.MaxTeams,
            cancellationToken);

        var league = await GetTrackedLeagueOrThrowAsync(id, cancellationToken);
        var commissioner = await _userRepository.GetResponseByIdAsync(
            league.CommissionerId,
            cancellationToken)
            ?? throw new NotFoundException(
                $"User '{league.CommissionerId}' was not found.");

        var draftDateUtc = DateTimeUtcConverter.ConvertToUtc(
            request.DraftDate,
            commissioner.TimeZoneId);
        draftDateUtc!.Value.ValidateFutureDraftDate();

        request.MapTo(league, draftDateUtc, commissioner.TimeZoneId);
        await _leagueRepository.SaveChangesAsync(cancellationToken);

        return league.ToResponse();
    }

    private async Task EnsureMaxTeamsCanBeUpdatedAsync(
        Guid leagueId,
        int maxTeams,
        CancellationToken cancellationToken)
    {
        var currentTeamCount = await _teamRepository.CountByLeagueIdAsync(
            leagueId,
            cancellationToken);

        if (maxTeams < currentTeamCount)
        {
            throw new ConflictException(
                $"MaxTeams cannot be lower than " +
                $"the current team count ({currentTeamCount}).");
        }
    }
}
