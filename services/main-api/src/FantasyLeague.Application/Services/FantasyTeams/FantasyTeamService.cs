using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Interfaces.Messaging;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Mappings;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed partial class FantasyTeamService(
    IFantasyTeamRepository _teamRepository,
    ILeagueRepository _leagueRepository,
    ILeagueSetupRepository _leagueSetupRepository,
    IUserRepository _userRepository,
    IIntegrationEventPublisher _eventPublisher
) : IFantasyTeamService
{
    public async Task<PagedResponse<FantasyTeamResponse>> GetByLeagueIdAsync(
        Guid leagueId,
        PaginationRequest req,
        CancellationToken cancellation = default
    )
    {
        req.ValidatePaginationRequest();

        await GetLeagueOrThrowAsync(leagueId, cancellation);

        var (items, totalCount) = await _teamRepository.GetPagedByLeagueIdAsync(
            leagueId,
            req,
            cancellation
        );

        return Pagination.CreateResponse(items, totalCount, req);
    }

    public async Task<FantasyTeamResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        var result = await _teamRepository.GetResponseByIdAsync(
            id,
            cancellation
        ) ?? throw new NotFoundException(
                $"Fantasy team '{id}' was not found."
            );

        return result;
    }

    public async Task RemoveLeagueMemberAsync(
        Guid leagueId,
        Guid teamId,
        CancellationToken cancellation = default)
    {
        await GetLeagueOrThrowAsync(leagueId, cancellation);
        await EnsureRegistrationIsOpenAsync(leagueId, cancellation);
        var team = await GetTrackedTeamOrThrowAsync(teamId, cancellation);

        if (team.LeagueId != leagueId)
        {
            throw new NotFoundException(
                $"Fantasy team '{teamId}' was not found in league '{leagueId}'.");
        }

        _teamRepository.Remove(team);
        await _teamRepository.SaveChangesAsync(cancellation);
    }

    public async Task<FantasyTeamResponse> UpdateAsync(
        Guid id,
        UpdateFantasyTeamRequest req,
        CancellationToken cancellation = default)
    {
        var team = await GetTrackedTeamOrThrowAsync(id, cancellation);

        req = req.NormalizeUpdateFantasyTeamRequest();
        req.ValidateUpdateFantasyTeamRequest();

        await EnsureUniqueAsync(
            team.LeagueId,
            team.OwnerId,
            req.Name,
            team.Id,
            cancellation
        );

        req.MapTo(team);

        await _teamRepository.SaveChangesAsync(cancellation);
        return team.ToResponse();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellation = default
    )
    {
        var team = await GetTrackedTeamOrThrowAsync(id, cancellation);
        await EnsureRegistrationIsOpenAsync(team.LeagueId, cancellation);
        _teamRepository.Remove(team);
        await _teamRepository.SaveChangesAsync(cancellation);
    }

}
