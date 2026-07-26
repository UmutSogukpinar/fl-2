using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;

namespace FantasyLeague.Application.Services.NbaPlayers;

public interface INbaPlayerService
{

    // <summary>
    // Gets the information based on the response size
    // of an NBA player by their unique identifier.
    // </summary>
    // <param name="id">The unique identifier of the NBA player.</param>
    // <returns>A task that represents the asynchronous operation
    // and returns the player information based on the provided identifier.
    // </returns>
    Task<IPlayerResponse> GetNbaPlayerByIdAndYearAsync(
        Guid id,
        int season,
        PlayerResponseSize responseSize,
        CancellationToken cancellationToken
    );

    //  =================================================================

}
