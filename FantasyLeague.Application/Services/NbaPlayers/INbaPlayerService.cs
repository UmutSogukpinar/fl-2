using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;

namespace FantasyLeague.Application.Services.NbaPlayers;

public interface INbaPlayerService
{
    Task<IPlayerResponse> GetNbaPlayerByIdAndYearAsync(Guid id, int season, PlayerResponseSize responseSize, CancellationToken cancellationToken);
}
