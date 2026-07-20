namespace FantasyLeague.Application.DTOs.Responses.NbaPlayers;

public sealed record NbaPlayerSyncResponse(
    int Season,
    int ProcessedCount,
    int CreatedCount,
    int UpdatedCount,
    int StatisticsProcessedCount);
