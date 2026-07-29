using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Domain.Entities;
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
        PaginationRequest req,
        CancellationToken cancellation
    )
    {
        await GetLeagueOrThrowAsync(leagueId, cancellation);

        var (items, totalCount) = await teamRepository.GetPagedByLeagueIdAsync(
            leagueId,
            req.PageNumber,
            req.PageSize,
            cancellation
        );

        return new PagedResponse<FantasyTeamResponse>(
            items,
            req.PageNumber,
            req.PageSize,
            totalCount,
            totalCount.CalculateTotalPage(req.PageSize));
    }

    public async Task<FantasyTeamResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await teamRepository.GetResponseByIdAsync(
            id,
            cancellationToken
        ) ?? throw new NotFoundException(
                $"Fantasy team '{id}' was not found."
            );

        return result; 
    }

    public async Task<FantasyTeamResponse> CreateAsync(
        CreateFantasyTeamRequest req,
        CancellationToken cancellation
    )
    {
        req.ValidateCreateFantasyTeamRequest();
        req = req.NormalizeCreateFantasyTeamRequest();

        var league = await GetLeagueOrThrowAsync(
            req.LeagueId,
            cancellation
        );

        if (league.Status is 
            LeagueStatus.Drafting or 
            LeagueStatus.Active or 
            LeagueStatus.Completed
        )
        {
            throw new ConflictException(
                "The league is no longer accepting members."
            );
        }

        _ = await userRepository.GetResponseByIdAsync(
                req.OwnerId,
                cancellation
        ) ?? throw new NotFoundException(
                $"User '{req.OwnerId}' was not found."
            );

        var teamCount = await teamRepository.CountByLeagueIdAsync(
            req.LeagueId, cancellation
        );

        if (teamCount >= league.MaxTeams)
        {
            throw new ConflictException(
                "The league has reached its team capacity."
            );
        }

        await EnsureUniqueAsync(
            req.LeagueId,
            req.OwnerId,
            req.Name,
            null,
            cancellation);

        var team = req.ToEntity();

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
        AddLeagueMemberRequest req,
        CancellationToken cancellation)
    {
        return CreateAsync(
            new CreateFantasyTeamRequest(req.TeamName, leagueId, req.OwnerId),
            cancellation);
    }

    public async Task<FantasyTeamResponse> JoinLeagueAsync(
        JoinLeagueRequest req,
        CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(req.JoinCode))
        {
            throw new BadRequestException("JoinCode is required.");
        }

        req = req.NormalizeJoinLeagueRequest();
        var league = await leagueRepository.GetResponseByJoinCodeAsync(
            req.JoinCode, cancellation)
            ?? throw new NotFoundException(
                "A league with the supplied join code was not found."
            );

        return await CreateAsync(
            new CreateFantasyTeamRequest(req.TeamName, league.Id, req.OwnerId),
            cancellation);
    }

    public async Task RemoveLeagueMemberAsync(
        Guid leagueId,
        Guid teamId,
        CancellationToken cancellation)
    {
        await GetLeagueOrThrowAsync(leagueId, cancellation);
        await EnsureRegistrationIsOpenAsync(leagueId, cancellation);
        var team = await GetTrackedTeamOrThrowAsync(teamId, cancellation);

        if (team.LeagueId != leagueId)
        {
            throw new NotFoundException(
                $"Fantasy team '{teamId}' was not found in league '{leagueId}'.");
        }

        teamRepository.Remove(team);
        await teamRepository.SaveChangesAsync(cancellation);
    }

    public async Task<FantasyTeamResponse> UpdateAsync(
        Guid id,
        UpdateFantasyTeamRequest req,
        CancellationToken cancellation)
    {
        var team = await GetTrackedTeamOrThrowAsync(id, cancellation);

        req.ValidateUpdateFantasyTeamRequest();
        req = req.NormalizeUpdateFantasyTeamRequest();

        await EnsureUniqueAsync(
            team.LeagueId,
            team.OwnerId,
            req.Name,
            team.Id,
            cancellation
        );

        req.MapTo(team);

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
                "League membership cannot change after" +
                "fixtures and draft order are generated.");
        }
    }

    // TODO update
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
