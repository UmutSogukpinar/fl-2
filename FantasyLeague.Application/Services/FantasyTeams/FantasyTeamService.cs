using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed class FantasyTeamService(
    IFantasyTeamRepository teamRepository,
    ILeagueRepository leagueRepository,
    ILeagueSetupRepository leagueSetupRepository,
    IUserRepository userRepository
) : IFantasyTeamService
{
    public async Task<PagedResponse<FantasyTeamResponse>> GetByLeagueIdAsync(
        Guid leagueId,
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetLeagueOrThrowAsync(leagueId, cancellationToken);
        var (items, totalCount) = await teamRepository.GetPagedByLeagueIdAsync(
            leagueId, request.PageNumber, request.PageSize, cancellationToken);
        return new PagedResponse<FantasyTeamResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize));
    }

    public async Task<FantasyTeamResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await teamRepository.GetResponseByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Fantasy team '{id}' was not found.");
    }

    public async Task<FantasyTeamResponse> CreateAsync(
        CreateFantasyTeamRequest request,
        CancellationToken cancellation
    )
    {
        FantasyTeamValidation.ValidateCreateUserRequest(request);

        FantasyTeamNormalization.NormalizeCreateUserRequest(ref request);
        var league = await GetLeagueOrThrowAsync(
            request.LeagueId,
            cancellation
        );
        if (league.Status is LeagueStatus.Drafting or LeagueStatus.Active or LeagueStatus.Completed)
        {
            throw new ConflictException("The league is no longer accepting members.");
        }
        _ = await userRepository.GetResponseByIdAsync(
                request.OwnerId,
                cancellation
        ) ?? throw new NotFoundException(
                $"User '{request.OwnerId}' was not found."
            );

        var teamCount = await teamRepository.CountByLeagueIdAsync(
            request.LeagueId, cancellation
        );

        if (teamCount >= league.MaxTeams)
        {
            throw new ConflictException(
                "The league has reached its team capacity."
            );
        }

        await EnsureUniqueAsync(
            request.LeagueId,
            request.OwnerId,
            request.Name,
            null,
            cancellation);

        var team = request.ToEntity();

        await teamRepository.AddAsync(team, cancellation);

        if (teamCount + 1 == league.MaxTeams
            && !await leagueSetupRepository.ExistsAsync(league.Id, cancellation))
        {
            var existingTeamIds = await teamRepository.GetIdsByLeagueIdAsync(
                league.Id, cancellation);
            var randomOrder = LeagueSetupGenerator.CreateRandomTeamOrder(
                existingTeamIds.Append(team.Id));
            var fixtures = LeagueSetupGenerator.CreateDoubleRoundRobinFixtures(
                league.Id, randomOrder);
            var draftOrder = LeagueSetupGenerator.CreateSnakeDraftOrder(
                league.Id, randomOrder, league.RosterSize);

            await leagueSetupRepository.AddAsync(fixtures, draftOrder, cancellation);
        }

        await teamRepository.SaveChangesAsync(cancellation);
        return team.ToResponse();
    }

    public Task<FantasyTeamResponse> AddLeagueMemberAsync(
        Guid leagueId,
        AddLeagueMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        return CreateAsync(
            new CreateFantasyTeamRequest(request.TeamName, leagueId, request.OwnerId),
            cancellationToken);
    }

    public async Task<FantasyTeamResponse> JoinLeagueAsync(
        JoinLeagueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.JoinCode))
        {
            throw new BadRequestException("JoinCode is required.");
        }

        var joinCode = request.JoinCode.Trim().ToUpperInvariant();
        var league = await leagueRepository.GetResponseByJoinCodeAsync(
            joinCode, cancellationToken)
            ?? throw new NotFoundException("A league with the supplied join code was not found.");

        return await CreateAsync(
            new CreateFantasyTeamRequest(request.TeamName, league.Id, request.OwnerId),
            cancellationToken);
    }

    public async Task RemoveLeagueMemberAsync(
        Guid leagueId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        await GetLeagueOrThrowAsync(leagueId, cancellationToken);
        await EnsureRegistrationIsOpenAsync(leagueId, cancellationToken);
        var team = await GetTrackedTeamOrThrowAsync(teamId, cancellationToken);

        if (team.LeagueId != leagueId)
        {
            throw new NotFoundException(
                $"Fantasy team '{teamId}' was not found in league '{leagueId}'.");
        }

        teamRepository.Remove(team);
        await teamRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<FantasyTeamResponse> UpdateAsync(
        Guid id,
        UpdateFantasyTeamRequest request,
        CancellationToken cancellation)
    {
        var team = await GetTrackedTeamOrThrowAsync(id, cancellation);

        FantasyTeamValidation.ValidateUpdateUserRequest(request);
        FantasyTeamNormalization.NormalizeUpdateUserRequest(ref request);

        await EnsureUniqueAsync(
            team.LeagueId,
            team.OwnerId,
            request.Name,
            team.Id,
            cancellation
        );

        request.MapTo(team);

        await teamRepository.SaveChangesAsync(cancellation);
        return team.ToResponse();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        var team = await GetTrackedTeamOrThrowAsync(id, cancellation);
        await EnsureRegistrationIsOpenAsync(team.LeagueId, cancellation);
        teamRepository.Remove(team);
        await teamRepository.SaveChangesAsync(cancellation);
    }

    private async Task EnsureRegistrationIsOpenAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        if (await leagueSetupRepository.ExistsAsync(leagueId, cancellationToken))
        {
            throw new ConflictException(
                "League membership cannot change after fixtures and draft order are generated.");
        }
    }

    private async Task EnsureUniqueAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellation)
    {
        if (await teamRepository.ExistsAsync(
                leagueId,
                ownerId,
                name,
                excludedTeamId,
                cancellation))
        {
            throw new ConflictException(
                "The owner already has a team " +
                "or the team name is already used in this league.");
        }
    }

    private async Task<LeagueResponse> GetLeagueOrThrowAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return await leagueRepository.GetResponseByIdAsync(id, cancellation)
            ?? throw new NotFoundException(
                    $"League '{id}' was not found."
                );
    }

    private async Task<FantasyTeam> GetTrackedTeamOrThrowAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return await teamRepository.GetTrackedByIdAsync(id, cancellation)
            ?? throw new NotFoundException(
                    $"Fantasy team '{id}' was not found."
                );
    }
}
