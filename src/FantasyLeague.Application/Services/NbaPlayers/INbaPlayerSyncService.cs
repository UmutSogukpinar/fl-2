using FantasyLeague.Application.DTOs.Responses.NbaPlayers;

namespace FantasyLeague.Application.Services.NbaPlayers;

public interface INbaPlayerSyncService
{
    Task<NbaPlayerSyncResponse> SyncActivePlayersAsync(CancellationToken cancellationToken = default);
}
