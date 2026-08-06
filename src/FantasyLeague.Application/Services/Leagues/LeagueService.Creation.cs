using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Users;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Time;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService
{
    public async Task<LeagueResponse> CreateAsync(
        CreateLeagueRequest request,
        CancellationToken cancellation = default)
    {
        request = request.NormalizeCreateLeagueRequest();
        request.ValidateCreateLeagueRequest();

        var commissioner = await _userRepository.GetResponseByIdAsync(
            request.CommissionerId,
            cancellation)
            ?? throw new NotFoundException(
                $"User '{request.CommissionerId}' was not found.");

        var draftDateUtc = DateTimeUtcConverter.ConvertToUtc(
            request.DraftDate,
            commissioner.TimeZoneId
        );

        draftDateUtc!.Value.ValidateFutureDraftDate();

        var league = request.ToEntity(draftDateUtc, commissioner.TimeZoneId);
        var commissionerTeam = CreateCommissionerTeam(
            league,
            request,
            commissioner.Username
        );

        await PersistLeagueAsync(
            league,
            commissionerTeam,
            cancellation
        );

        return league.ToResponse();
    }

    private static FantasyTeam CreateCommissionerTeam(
        League league,
        CreateLeagueRequest request,
        string commissionerUsername)
    {
        return new FantasyTeam
        {
            Name = request.TeamName.GetCommissionerTeamName(
                commissionerUsername),
            LeagueId = league.Id,
            OwnerId = request.CommissionerId
        };
    }

    private async Task PersistLeagueAsync(
        League league,
        FantasyTeam commissionerTeam,
        CancellationToken cancellation)
    {
        await _leagueRepository.AddAsync(league, cancellation);
        await _teamRepository.AddAsync(commissionerTeam, cancellation);
        await _leagueRepository.SaveChangesAsync(cancellation);
    }
}
