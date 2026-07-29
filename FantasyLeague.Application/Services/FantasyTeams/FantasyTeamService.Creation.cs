using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed partial class FantasyTeamService
{
    private async Task<FantasyTeamResponse> CreateTeamAsync(
        CreateFantasyTeamRequest req,
        CancellationToken cancellation = default)
    {
        req = req.NormalizeCreateFantasyTeamRequest();
        req.ValidateCreateFantasyTeamRequest();

        var league = await GetLeagueOrThrowAsync(req.LeagueId, cancellation);

        if (league.Status is
            LeagueStatus.Drafting or
            LeagueStatus.Active or
            LeagueStatus.Completed)
        {
            throw new ConflictException(
                "The league is no longer accepting members.");
        }

        _ = await _userRepository.GetResponseByIdAsync(req.OwnerId, cancellation)
            ?? throw new NotFoundException($"User '{req.OwnerId}' was not found.");

        var teamCount = await _teamRepository.CountByLeagueIdAsync(
            req.LeagueId, cancellation);

        if (teamCount >= league.MaxTeams)
        {
            throw new ConflictException(
                "The league has reached its team capacity.");
        }

        await EnsureUniqueAsync(
            req.LeagueId,
            req.OwnerId,
            req.Name,
            null,
            cancellation);

        var team = req.ToEntity();
        await _teamRepository.AddAsync(team, cancellation);
        await _teamRepository.SaveChangesAsync(cancellation);

        return team.ToResponse();
    }

    public Task<FantasyTeamResponse> AddLeagueMemberAsync(
        Guid leagueId,
        AddLeagueMemberRequest req,
        CancellationToken cancellation = default)
    {
        return CreateTeamAsync(
            new CreateFantasyTeamRequest(req.TeamName, leagueId, req.OwnerId),
            cancellation);
    }

    public async Task<FantasyTeamResponse> JoinLeagueAsync(
        JoinLeagueRequest req,
        CancellationToken cancellation = default)
    {
        req = req.NormalizeJoinLeagueRequest();
        req.ValidateJoinLeagueRequest();

        var league = await _leagueRepository.GetResponseByJoinCodeAsync(
            req.JoinCode, cancellation)
            ?? throw new NotFoundException(
                "A league with the supplied join code was not found.");

        return await CreateTeamAsync(
            new CreateFantasyTeamRequest(req.TeamName, league.Id, req.OwnerId),
            cancellation);
    }
}
