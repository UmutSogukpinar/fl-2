using FantasyLeague.Application.DTOs.Responses.Leagues;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService
{
    public async Task<IReadOnlyList<LeagueStandingResponse>> GetStandingsAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        await GetLeagueOrThrowAsync(id, cancellation);
        return await _leagueSetupRepository.GetStandingsAsync(id, cancellation);
    }

    public async Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        _ = await GetLeagueOrThrowAsync(id, cancellation);

        return await _leagueSetupRepository.GetFixturesAsync(
            id,
            cancellation
        );
    }

    public async Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        _ = await GetLeagueOrThrowAsync(id, cancellation);

        return await _leagueSetupRepository.GetDraftOrderAsync(
            id,
            cancellation
        );
    }
}
