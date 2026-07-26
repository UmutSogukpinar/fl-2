using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;

namespace FantasyLeague.Application.Services.NbaPlayers;

public sealed class NbaPlayerService(INbaPlayerRepository _nbaPlayerRepository) 
    : INbaPlayerService
{
    public async Task<IPlayerResponse> GetNbaPlayerByIdAndYearAsync(
        Guid id,
        int season,
        PlayerResponseSize size,
        CancellationToken cancellation
    ){
        return await _nbaPlayerRepository.GetByIdAndSeasonAsync(
            id,
            season,
            size,
            cancellation)
            ?? throw new NotFoundException($"NBA player '{id}' was not found.");
    }
}
