using FantasyLeague.Application.DTOs.Responses.Leagues;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService
{
    public async Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _ = await GetLeagueOrThrowAsync(id, cancellationToken);

        return await _leagueSetupRepository.GetFixturesAsync(
            id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _ = await GetLeagueOrThrowAsync(id, cancellationToken);

        return await _leagueSetupRepository.GetDraftOrderAsync(
            id,
            cancellationToken);
    }
}
