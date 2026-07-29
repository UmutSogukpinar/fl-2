using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;

namespace FantasyLeague.Application.Services.NbaPlayers;

public interface INbaPlayerService
{
    Task<PagedResponse<NbaPlayerBasicResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<IPlayerResponse> GetNbaPlayerByIdAndYearAsync(Guid id, int season, PlayerResponseSize responseSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<IPlayerResponse>> GetNbaPlayersByNameAndYearAsync(PaginationRequest pagination, GetNbaPlayersRequest request, CancellationToken cancellationToken = default);
}
