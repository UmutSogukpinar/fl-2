using FantasyLeague.Application.Common.Interfaces.ExternalServices;
using Microsoft.Extensions.Options;

namespace FantasyLeague.Infrastructure.ExternalServices.NbaApi;

public sealed class ApiSportsClient : INbaPlayersApiClient
{
    private readonly ApiSportsRequestClient _requestClient;
    private IReadOnlyCollection<ApiTeam>? _teams;

    public ApiSportsClient(HttpClient httpClient, IOptions<ApiSportsOptions> options)
    {
        _requestClient = new ApiSportsRequestClient(httpClient, options.Value);
    }

    public async Task<IReadOnlyCollection<ExternalNbaPlayer>> GetActivePlayersAsync(
        int season,
        CancellationToken cancellationToken)
    {
        var players = new Dictionary<int, ExternalNbaPlayer>();

        foreach (var team in await GetNbaTeamsAsync(cancellationToken))
        {
            var apiPlayers = await _requestClient.GetResponseAsync<ApiPlayer>(
                $"/players?team={team.Id}&season={season}", cancellationToken);

            foreach (var player in apiPlayers.Where(ApiSportsMapper.IsActivePlayer))
            {
                players[player.Id] = ApiSportsMapper.ToExternalPlayer(player, team.Code);
            }
        }

        return players.Values.ToArray();
    }

    public async Task<IReadOnlyCollection<ExternalPlayerGameStats>> GetPlayerStatisticsAsync(
        int season,
        CancellationToken cancellationToken)
    {
        var statistics = new List<ExternalPlayerGameStats>();

        foreach (var team in await GetNbaTeamsAsync(cancellationToken))
        {
            var apiStatistics = await _requestClient.GetResponseAsync<ApiPlayerStats>(
                $"/players/statistics?team={team.Id}&season={season}", cancellationToken);
            statistics.AddRange(apiStatistics.Select(ApiSportsMapper.ToExternalStats));
        }

        return statistics;
    }

    private async Task<IReadOnlyCollection<ApiTeam>> GetNbaTeamsAsync(
        CancellationToken cancellationToken)
    {
        if (_teams is not null) return _teams;

        var teams = await _requestClient.GetResponseAsync<ApiTeam>("/teams", cancellationToken);
        _teams = teams.Where(team => team.NbaFranchise && !team.AllStar).ToArray();
        return _teams;
    }
}
