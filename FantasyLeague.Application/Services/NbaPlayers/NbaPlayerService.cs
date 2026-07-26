using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Mappings;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;

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
        return size switch
        {
            PlayerResponseSize.Basic => await GetBasicPlayerOrThrowAsync(
                id, season, cancellation
            ),
            PlayerResponseSize.Detailed => await GetDetailedPlayerOrThrowAsync(
                id, season, cancellation
            ),
            PlayerResponseSize.Extended => await GetExtendedPlayerOrThrowAsync(
                id, season, cancellation
            ),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(size), size, "Invalid player response size."
                 )
        };
    }


    // ================= Utils of GetNbaPlayerAsync() =================
    private async Task<NbaPlayerBasicResponse> GetBasicPlayerOrThrowAsync(
        Guid id,
        int season,
        CancellationToken cancellationToken
    ){
        var player = await _nbaPlayerRepository.GetByIdAndSeasonAsync(
            id, season, PlayerResponseSize.Basic, cancellationToken)
            ?? throw new NotFoundException($"NBA player '{id}' was not found.");

        return NbaPlayerMappings.ToBasicResponse(player);
    }

    private async Task<NbaPlayerDetailedResponse> GetDetailedPlayerOrThrowAsync(
        Guid id,
        int season,
        CancellationToken cancellationToken
    )
    {
        var player = await _nbaPlayerRepository.GetByIdAndSeasonAsync(
            id, season, PlayerResponseSize.Detailed, cancellationToken)
            ?? throw new NotFoundException($"NBA player '{id}' was not found.");

        return NbaPlayerMappings.ToDetailedResponse(player);
    }

    private async Task<NbaPlayerExtendedResponse> GetExtendedPlayerOrThrowAsync(
        Guid id,
        int season,
        CancellationToken cancellationToken
    )
    {
        var player = await _nbaPlayerRepository.GetByIdAndSeasonAsync(
            id, season, PlayerResponseSize.Extended, cancellationToken
            ) ?? throw new NotFoundException(
                    $"NBA player '{id}' was not found."
                 );

        return NbaPlayerMappings.ToExtendedResponse(player);
    }
}
