using FantasyLeague.Application.Common.Interfaces.Repositories;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService(
    ILeagueRepository leagueRepository,
    IFantasyTeamRepository teamRepository,
    ILeagueSetupRepository leagueSetupRepository,
    IDraftRepository draftRepository) : IDraftService
{
    private const int MinimumTeamCount = 2;
    private static readonly TimeSpan PickDuration = TimeSpan.FromSeconds(60);
}
