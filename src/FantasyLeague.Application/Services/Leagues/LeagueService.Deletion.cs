using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService
{
    public async Task DeleteAsync(
        Guid id,
        Guid commissionerId,
        CancellationToken cancellationToken = default)
    {
        var league = await GetTrackedLeagueOrThrowAsync(id, cancellationToken);

        EnsureCommissionerCanDelete(league, commissionerId);
        EnsureLeagueCanBeDeleted(league);

        _leagueRepository.Remove(league);
        await _leagueRepository.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureCommissionerCanDelete(
        League league,
        Guid commissionerId)
    {
        if (league.CommissionerId != commissionerId)
        {
            throw new ForbiddenException(
                "Only the league commissioner can cancel the league.");
        }
    }

    private static void EnsureLeagueCanBeDeleted(League league)
    {
        if (league.Status is not (
            LeagueStatus.Created or
            LeagueStatus.RegistrationOpen or
            LeagueStatus.DraftDelayed))
        {
            throw new ConflictException(
                "Only a created, registration-open, " +
                "or delayed league can be cancelled."
            );
        }
    }
}
