using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.FantasyTeams;

public sealed partial class FantasyTeamService
{
    private async Task EnsureRegistrationIsOpenAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        if (await leagueSetupRepository.ExistsAsync(leagueId, cancellationToken))
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
        var conflict = await teamRepository.ExistsAsync(
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
        return await leagueRepository.GetResponseByIdAsync(id, cancellation)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    private async Task<FantasyTeam> GetTrackedTeamOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await teamRepository.GetTrackedByIdAsync(id, cancellation)
            ?? throw new NotFoundException($"Fantasy team '{id}' was not found.");
    }
}
