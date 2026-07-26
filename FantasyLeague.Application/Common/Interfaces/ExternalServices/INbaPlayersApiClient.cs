namespace FantasyLeague.Application.Common.Interfaces.ExternalServices;

public interface INbaPlayersApiClient
{
    Task<IReadOnlyCollection<ExternalNbaPlayer>> GetActivePlayersAsync(int season, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ExternalPlayerGameStats>> GetPlayerStatisticsAsync(int season, CancellationToken cancellationToken);
}

public sealed record ExternalNbaPlayer(int NbaId, string FirstName, string LastName, string? Team, string? Position, int? JerseyNumber, int? HeightCm, decimal? WeightKg);

public sealed record ExternalPlayerGameStats(int NbaPlayerId, int GameId, string? Team, string? Position, decimal Minutes, int Points, int Rebounds, int Assists, int Steals, int Blocks, int Turnovers, int FieldGoalsMade, int FieldGoalsAttempted, int ThreePointersMade, int ThreePointersAttempted, int FreeThrowsMade, int FreeThrowsAttempted);
