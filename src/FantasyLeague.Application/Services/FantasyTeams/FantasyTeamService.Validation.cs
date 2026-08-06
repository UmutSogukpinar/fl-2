using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Users;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed partial class FantasyTeamService
{
    private static void EnsureLeagueAcceptsMembers(LeagueResponse league)
    {
        if (league.Status is
            LeagueStatus.Drafting or
            LeagueStatus.Active or
            LeagueStatus.Completed)
        {
            throw new ConflictException(
                "The league is no longer accepting members.");
        }
    }

    private async Task EnsureOwnerExistsAsync(
        Guid ownerId,
        CancellationToken cancellation)
    {
        _ = await _userRepository.GetResponseByIdAsync(ownerId, cancellation)
            ?? throw new NotFoundException($"User '{ownerId}' was not found.");
    }

    private async Task EnsureLeagueHasCapacityAsync(
        LeagueResponse league,
        CancellationToken cancellation)
    {
        var teamCount = await _teamRepository.CountByLeagueIdAsync(
            league.Id, cancellation);

        if (teamCount >= league.MaxTeams)
        {
            throw new ConflictException(
                "The league has reached its team capacity.");
        }
    }

    private async Task EnsureRegistrationIsOpenAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        if (await _leagueSetupRepository.DraftOrderExistsAsync(
                leagueId, cancellationToken))
        {
            throw new ConflictException(
                "League membership cannot change after " +
                "fixtures and draft order are generated.");
        }
    }

    private async Task EnsureUniqueAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellation)
    {
        var conflict = await _teamRepository.ExistsAsync(
            leagueId,
            ownerId,
            name,
            excludedTeamId,
            cancellation);

        var message = conflict switch
        {
            FastasyTeamConflictResult.OwnerHasMultipleTeam =>
                "The owner already has a team in this league.",
            FastasyTeamConflictResult.NameIsTaken =>
                "The team name is already used in this league.",
            FastasyTeamConflictResult.OwnerHasMultipleTeam |
                FastasyTeamConflictResult.NameIsTaken =>
                "The owner already has a team and the team name is already used in this league.",
            _ => null
        };

        if (message is not null)
        {
            throw new ConflictException(message);
        }
    }

    private async Task<LeagueResponse> GetLeagueOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await _leagueRepository.GetResponseByIdAsync(id, cancellation)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    private async Task<FantasyTeam> GetTrackedTeamOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await _teamRepository.GetTrackedByIdAsync(id, cancellation)
            ?? throw new NotFoundException($"Fantasy team '{id}' was not found.");
    }
}
