using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed class FantasyTeamService(
    IFantasyTeamRepository teamRepository,
    ILeagueRepository leagueRepository,
    IUserRepository userRepository
) : IFantasyTeamService
{
    public async Task<IReadOnlyCollection<FantasyTeamResponse>> GetByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default)
    {
        await GetLeagueOrThrowAsync(leagueId, cancellationToken);
        var teams = await teamRepository.GetByLeagueIdAsync(
            leagueId, cancellationToken
        );

        return teams.Select(team => team.ToResponse()).ToArray();
    }

    public async Task<FantasyTeamResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return (await GetTeamOrThrowAsync(
                id,
                cancellationToken))
            .ToResponse();
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
        var owner = await userRepository.GetByIdAsync(
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

        var team = request.ToEntity(league, owner);

        await teamRepository.AddAsync(team, cancellation);
        await teamRepository.SaveChangesAsync(cancellation);
        return team.ToResponse();
    }

    public async Task<FantasyTeamResponse> UpdateAsync(
        Guid id,
        UpdateFantasyTeamRequest request,
        CancellationToken cancellation)
    {
        var team = await GetTeamOrThrowAsync(id, cancellation);

        FantasyTeamValidation.ValidateUpdateUserRequest(request);
        FantasyTeamNormalization.NormalizeUpdateUserRequest(ref request);

        await EnsureUniqueAsync(
            team.LeagueId,
            team.OwnerId,
            team.Name,
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
        var team = await GetTeamOrThrowAsync(id, cancellation);
        teamRepository.Remove(team);
        await teamRepository.SaveChangesAsync(cancellation);
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

    private async Task<League> GetLeagueOrThrowAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return await leagueRepository.GetByIdAsync(id, cancellation) 
            ?? throw new NotFoundException(
                    $"League '{id}' was not found."
                );
    }

    private async Task<FantasyTeam> GetTeamOrThrowAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return await teamRepository.GetByIdAsync(id, cancellation)
            ?? throw new NotFoundException(
                    $"Fantasy team '{id}' was not found."
                );
    }
}
